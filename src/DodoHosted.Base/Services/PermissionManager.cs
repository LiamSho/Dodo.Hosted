// This file is a part of Dodo.Hosted project.
// 
// Copyright (C) 2022 LiamSho and all Contributors
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY

using DoDo.Open.Sdk.Models.Members;
using DodoHosted.Base.Interfaces;
using DodoHosted.Base.Models;
using MongoDB.Driver;
using MongoDB.Driver.Linq;

// ReSharper disable ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator

namespace DodoHosted.Base.Services;

public class PermissionManager : IPermissionManager
{
    private readonly IMongoCollection<PermissionSchema> _collection;

    public PermissionManager(IMongoDatabase database)
    {
        _collection = database.GetCollection<PermissionSchema>("system-permission-schema");
    }

    #region Describe Schema Check

    public Task<PermissionSchema?> DescribeSchemaCheck(string node, CommandMessage commandMessage)
    {
        return DescribeSchemaCheck(node,
            commandMessage.Roles
                .OrderByDescending(x => x.Position)
                .Select(x => x.Id)
                .ToList(),
            commandMessage.IslandId, commandMessage.ChannelId);
    }

    public Task<PermissionSchema?> DescribeSchemaCheck(string node, IEnumerable<MemberRole> roles, string islandId, string channelId)
    {
        return DescribeSchemaCheck(node,
            roles
                .OrderByDescending(x => x.Position)
                .Select(x => x.Id)
                .ToList(),
            islandId, channelId);
    }

    public Task<PermissionSchema?> DescribeSchemaCheck(string node, IEnumerable<GetMemberRoleListOutput> roles, string islandId, string channelId)
    {
        return DescribeSchemaCheck(node,
            roles
                .OrderByDescending(x => x.Position)
                .Select(x => x.RoleId)
                .ToList(),
            islandId, channelId);
    }

    private async Task<PermissionSchema?> DescribeSchemaCheck(string node, List<string> roles, string islandId, string channelId)
    {
        var status = await DescribeSchemaCheck(node, roles, islandId, channelId, true)
                     ?? await DescribeSchemaCheck(node, roles, islandId, channelId, false);

        return status;
    }

    private async Task<PermissionSchema?> DescribeSchemaCheck(string node, List<string> roles, string islandId, string channelId, bool islandMatch)
    {
        var nodes = node.Split(".").ToList();
        var isAsterisk = false;

        while (true)
        {
            if (nodes.Count == 0)
            {
                var allMatchSchema = islandMatch
                    ? await _collection
                        .AsQueryable()
                        .Where(x => x.Island == islandId)
                        .FirstOrDefaultAsync(x => x.Node == "*")
                    : await _collection
                        .AsQueryable()
                        .Where(x => x.Island == "*" && x.Channel == "*" && x.Role == "*")
                        .FirstOrDefaultAsync(x => x.Node == "*");

                return allMatchSchema;
            }

            var currentNode = string.Join(".", nodes);
            var schemas = islandMatch
                ? await _collection
                    .AsQueryable()
                    .Where(x => x.Island == islandId)
                    .Where(x => x.Node == currentNode)
                    .ToListAsync()
                : await _collection
                    .AsQueryable()
                    .Where(x => x.Island == "*" && x.Channel == "*" && x.Role == "*")
                    .Where(x => x.Node == currentNode)
                    .ToListAsync();

            if (schemas.Count == 0)
            {
                if (isAsterisk is false && node.Length != 1)
                {
                    isAsterisk = true;
                    nodes[^1] = "*";
                }
                else
                {
                    isAsterisk = false;
                    nodes.RemoveAt(nodes.Count - 1);
                }
                
                continue;
            }

            // stage => channel, role    matched schemas
            // 0     => xxx,     yyy     collection
            // 1     => xxx,     *       single
            // 2     => *,       yyy     collection
            // 3     => *,       *       single
            
            // stage 0
            var matchedSchemas = schemas
                .Where(x => x.Channel == channelId && x.Role != "*")
                .ToList();
            if (matchedSchemas.Count != 0)
            {
                foreach (var role in roles)
                {
                    var roleMatched = matchedSchemas.FirstOrDefault(x => x.Role == role);
                    if (roleMatched is not null)
                    {
                        return roleMatched;
                    }
                }
            }

            // stage 1
            var matchedSchema = schemas.FirstOrDefault(x => x.Channel == channelId && x.Role == "*");
            if (matchedSchema is not null)
            {
                return matchedSchema;
            }

            // stage 2
            matchedSchemas = schemas
                .Where(x => x.Channel == "*" && x.Role != "*")
                .ToList();
            if (matchedSchemas.Count != 0)
            {
                foreach (var role in roles)
                {
                    var roleMatched = matchedSchemas.FirstOrDefault(x => x.Role == role);
                    if (roleMatched is not null)
                    {
                        return roleMatched;
                    }
                }
            }
            
            // stage 3
            matchedSchema = schemas.FirstOrDefault(x => x.Channel == "*" && x.Role == "*");
            if (matchedSchema is not null)
            {
                return matchedSchema;
            }

            if (isAsterisk is false && node.Length != 1)
            {
                isAsterisk = true;
                nodes[^1] = "*";
            }
            else
            {
                isAsterisk = false;
                nodes.RemoveAt(nodes.Count - 1);
            }
        }
    }
    
    #endregion

    #region Check Permission

    public async Task<bool> CheckPermission(string node, CommandMessage commandMessage)
    {
        var schema = await DescribeSchemaCheck(node, commandMessage);
        return schema?.Value == "allow";
    }

    public async Task<bool> CheckPermission(string node, IEnumerable<MemberRole> roles, string islandId, string channelId)
    {
        var schema = await DescribeSchemaCheck(node, roles, islandId, channelId);
        return schema?.Value == "allow";
    }

    public async Task<bool> CheckPermission(string node, IEnumerable<GetMemberRoleListOutput> roles, string islandId, string channelId)
    {
        var schema = await DescribeSchemaCheck(node, roles, islandId, channelId);
        return schema?.Value == "allow";
    }

    #endregion

    #region Add Permission

    public async Task<PermissionSchema?> AddPermission(string node, string islandId, string channelId, string roleId, string value)
    {
        var exist = await _collection.FindAsync(x =>
            x.Node == node && x.Island == islandId && x.Channel == channelId && x.Role == roleId && x.Value == value);
        if (await exist.AnyAsync())
        {
            return null;
        }

        var schema = new PermissionSchema
        {
            Node = node,
            Island = islandId,
            Channel = channelId,
            Role = roleId,
            Value = value
        };

        await _collection.InsertOneAsync(schema);

        var added = await _collection.FindAsync(x => x.Id == schema.Id);
        return await added.FirstOrDefaultAsync();
    }

    #endregion

    #region Set Permission

    public async Task<(PermissionSchema?, PermissionSchema?)> SetPermissionSchema(string islandId, Guid id, string? channel = null, string? role = null, string? value = null)
    {
        var original = await _collection
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == id && x.Island == islandId);
        if (original is null)
        {
            return (null, null);
        }
        
        var updates = new List<UpdateDefinition<PermissionSchema>>();
        if (channel is not null)
        {
            updates.Add(Builders<PermissionSchema>.Update.Set(x => x.Channel, channel));
        }
        if (role is not null)
        {
            updates.Add(Builders<PermissionSchema>.Update.Set(x => x.Role, role));
        }
        if (value is not null)
        {
            updates.Add(Builders<PermissionSchema>.Update.Set(x => x.Value, value));
        }

        var update = Builders<PermissionSchema>.Update.Combine(updates);

        await _collection.UpdateOneAsync(x => x.Id == id && x.Island == islandId, update);

        var updated = await _collection
            .AsQueryable()
            .FirstOrDefaultAsync(x => x.Id == id && x.Island == islandId);

        return (original, updated);
    }

    #endregion
    
    #region Get Permission

    public async Task<List<PermissionSchema>> GetPermissionSchemas(string islandId, string? channelId = null, string? roleId = null)
    {
        var queryable = _collection
            .AsQueryable()
            .Where(x => x.Island == islandId);

        if (channelId is not null)
        {
            queryable = queryable.Where(x => x.Channel == "*" || x.Channel == channelId);
        }

        if (roleId is not null)
        {
            queryable = queryable.Where(x => x.Role == "*" || x.Role == roleId);
        }
        
        var schemas = await queryable.ToListAsync();
        
        return schemas;
    }

    #endregion

    #region Remove Permission Schema

    public async Task<PermissionSchema?> RemovePermissionSchemaById(string islandId, Guid id, bool dryRun = false)
    {
        if (dryRun)
        {
            return await _collection
                .AsQueryable()
                .FirstOrDefaultAsync(x => x.Id == id && x.Island == islandId);
        }
        
        return await _collection.FindOneAndDeleteAsync(x => x.Id == id && x.Island == islandId);
    }

    public async Task<List<PermissionSchema>> RemovePermissionSchemasByNode(string islandId, string node, bool dryRun = false)
    {
        var deleted = _collection
            .AsQueryable()
            .Where(x => x.Node == node && x.Island == islandId)
            .ToList();

        if (dryRun)
        {
            return deleted;
        }
        
        await _collection.DeleteManyAsync(x => x.Node == node && x.Island == islandId);

        return deleted;
    }

    public async Task<List<PermissionSchema>> RemovePermissionSchemasBySearch(string islandId, string? channel = null, string? role = null, bool dryRun = false)
    {
        var schemas = await GetPermissionSchemas(islandId, channel, role);

        if (dryRun)
        {
            return schemas;
        }

        var ids = schemas.Select(x => x.Id).ToList();
        await _collection.DeleteManyAsync(x => ids.Contains(x.Id));

        return schemas;
    }

    #endregion
}
