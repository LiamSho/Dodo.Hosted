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

using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Members;
using DoDo.Open.Sdk.Models.Roles;
using DodoHosted.Base.App.Attributes;
using DodoHosted.Base.App.Context;
using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.Card.CardComponent;
using DodoHosted.Base.Card.Enums;
using DodoHosted.Base.Context;
using DodoHosted.Lib.Plugin.Cards;

namespace DodoHosted.Lib.Plugin.Builtin;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

public sealed class PermissionManagerCommand : ICommandExecutor
{
    public async Task<bool> AddPermission(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("node", "n", "权限节点")] string node,
        [CmdOption("channel", "c", "适用频道")] DodoChannelIdWithWildcard channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`")] string roleId,
        [CmdOption("value", "v", "值，可为 `allow` 或 `deny`")] string value)
    {
        // 若节点有 *，则只能是最后一位为 *
        if (node.Contains('*'))
        {
            if (node.EndsWith("*") is false)
            {
                await context.Reply.Invoke("Node 不合法");
                return false;
            }
        }
         
        // 检查 Role
        if (roleId != "*" && long.TryParse(roleId, out _) is false)
        {
            await context.Reply.Invoke("Role 不合法");
            return false;
        }
         
        // 检查 Value
        if (value is not ("allow" or "deny"))
        { 
            await context.Reply.Invoke("Value 不合法");
            return false;
        }

        var result = await pm.AddPermission(node, context.EventInfo.IslandId, channelId.Value, roleId, value);

        if (result is null)
        {
            await context.Reply.Invoke("添加权限节点失败，可能已经存在值相同的同名节点");
            return false;
        }

        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        var card = result.GetPermissionSingleCard("添加权限节点", roles, channels);
        card.Card.Theme = CardTheme.Green;

        await context.ReplyCard.Invoke(card);
        return true;
    }

    public async Task<bool> SetPermission(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("id", "i", "权限节点的 GUID")] string nodeId,
        [CmdOption("channel", "c", "适用频道", false)] DodoChannelIdWithWildcard? channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`", false)] string? roleId,
        [CmdOption("value", "v", "值，可为 `allow` 或 `deny`", false)] string? value)
    {
        var guidParsed = Guid.TryParse(nodeId, out var parsedGuid);
        if (guidParsed is false)
        {
            await context.Reply.Invoke("Guid 不合法");
            return false;
        }
        
        if (roleId is not null)
        {
            if (roleId != "*" && long.TryParse(roleId, out _) is false)
            {
                await context.Reply.Invoke("Role 不合法");
                return false;
            }  
        }

        if (value is not null)
        {
            if (value is not ("allow" or "deny"))
            {
                await context.Reply.Invoke("Value 不合法");
                return false;
            }
        }

        var (ori, upt) = await pm
            .SetPermissionSchema(context.EventInfo.IslandId, parsedGuid, channelId?.Value, roleId, value);

        if (ori is null)
        {
            await context.Reply.Invoke($"找不到 ID 为 `{nodeId}` 的记录");
            return false;
        }

        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        var card = PermissionManagerMessageCard.GetPermissionChangeResultCard(ori, upt!, roles.ToList(), channels.ToList());
        
        await context.ReplyCard.Invoke(card);
        return true;
    }

    public async Task<bool> RemovePermissionById(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("id", "i", "权限节点的 GUID")] string nodeId,
        [CmdOption("dry-run", "d", "Dry Run", false)] bool? isDryRun)
    {
        var dryRun = isDryRun ?? false;
        
        var guidParsed = Guid.TryParse(nodeId, out var parsedGuid);
        if (guidParsed is false)
        {
            await context.Reply.Invoke("Guid 不合法");
            return false;
        }
        
        var removePermissionSchema = await pm.RemovePermissionSchemaById(context.EventInfo.IslandId, parsedGuid, dryRun);

        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        var card = removePermissionSchema.GetPermissionSingleCard("移除的权限", roles, channels);
        if (dryRun)
        {
            card.AddComponent(new Remark(new Text("运行模式：***DRY RUN***")));
        }

        card.Card.Theme = dryRun ? CardTheme.Orange : CardTheme.Purple;

        await context.ReplyCard.Invoke(card);
        return true;
    }

    public async Task<bool> RemovePermissionByNode(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("node", "n", "权限节点")] string node,
        [CmdOption("dry-run", "d", "Dry Run", false)] bool? isDryRun)
    {
        var dryRun = isDryRun ?? false;
        
        var removePermissionSchema = await pm.RemovePermissionSchemasByNode(context.EventInfo.IslandId, node, dryRun);
        
        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        var card = removePermissionSchema.GetPermissionListCard("移除的权限列表", roles, channels);
        if (dryRun)
        {
            card.AddComponent(new Remark(new Text("运行模式：***DRY RUN***")));
        }

        card.Card.Theme = dryRun ? CardTheme.Orange : CardTheme.Purple;

        await context.ReplyCard.Invoke(card);
        return true;
    }
    
    public async Task<bool> RemovePermissionBySearch(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("channel", "c", "适用频道")] DodoChannelIdWithWildcard channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`")] string roleId,
        [CmdOption("dry-run", "d", "Dry Run", false)] bool? isDryRun)
    {
        var dryRun = isDryRun ?? false;
        
        var removePermissionSchema = await pm.RemovePermissionSchemasBySearch(context.EventInfo.IslandId, channelId.Value, roleId, dryRun);
        if (removePermissionSchema.Count == 0)
        {
            await context.Reply.Invoke("未找到匹配的权限记录");
            return true;
        }

        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        var card = removePermissionSchema.GetPermissionListCard("移除的权限列表", roles, channels);
        if (dryRun)
        {
            card.AddComponent(new Remark(new Text("运行模式：***DRY RUN***")));
        }

        card.Card.Theme = dryRun ? CardTheme.Orange : CardTheme.Purple;

        await context.ReplyCard.Invoke(card);
        return true;
    }

    public async Task<bool> ListPermissions(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("channel", "c", "适用频道", false)] DodoChannelIdWithWildcard? channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`", false)] string? roleId,
        [CmdOption("page", "p", "页码", false)] int? page)
    {
        if (roleId is not null)
        {
            if (roleId != "*" && long.TryParse(roleId, out _) is false)
            {
                await context.Reply.Invoke("Role 不合法");
                return false;
            }
        }

        const int PageSize = 10;

        var p = Math.Max(page ?? 1, 1);

        var allPerms = await pm.GetPermissionSchemas(context.EventInfo.IslandId, channelId?.Value, roleId);
        var perms = allPerms
            .OrderBy(x => x.Id)
            .Skip(PageSize * (p - 1))
            .Take(PageSize)
            .ToArray();
        
        var pages = (int)Math.Ceiling(allPerms.Count / (double)PageSize);

        if (perms.Length == 0)
        {
            var msg = p == 1 ? "找不到符合检索条件的权限组" : $"没有更多的内容了，最大页码 {pages}";
            await context.Reply.Invoke(msg);
            return true;
        }

        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        var card = perms.GetPermissionListCard("权限列表", roles, channels, pages, p);

        await context.ReplyCard.Invoke(card);
        return true;
    }

    public async Task<bool> CheckPermission(
        CommandContext context,
        [Inject] IPermissionManager pm,
        [Inject] OpenApiService openApiService,
        [CmdOption("node", "n", "权限节点")] string node,
        [CmdOption("channel", "c", "适用频道")] DodoChannelId channelId, 
        [CmdOption("user", "u", "用户")] DodoMemberId memberId)
    {
        // 检查 Node
        if (node.Contains('*'))
        {
            await context.Reply.Invoke("Node 不可包含 `*`");
            return false;
        }
        
        var memberRoles = await openApiService.GetMemberRoleListAsync(new GetMemberRoleListInput
        {
            IslandId = context.EventInfo.IslandId, DodoId = memberId.Value
        });
        
        if (memberRoles is null)
        {
            await context.Reply.Invoke("获取用户身份组列表失败");
            return false;
        }
        
        var (roles, channels) =
            await GetBasicInfos(openApiService, context.EventInfo.IslandId);
        
        var result = await pm.DescribeSchemaCheck(node, memberRoles, context.EventInfo.IslandId, channelId.Value);
        var card = result.GetPermissionCheckResultCard(roles, channels);
        
        await context.ReplyCard.Invoke(card);
        return true;
    }

    public CommandTreeBuilder GetBuilder()
    {
        return new CommandTreeBuilder("pm", "权限管理器", "system.permission")
            .Then("add", "添加权限节点", "modify", AddPermission)
            .Then("set", "修改权限节点", "modify", SetPermission)
            .Then("remove", "移除权限节点", "modify", builder: x => x
                .Then("single", "根据 ID 移除单个权限节点", string.Empty, RemovePermissionById)
                .Then("nodes", "根据节点匹配移除多个权限节点", string.Empty, RemovePermissionByNode)
                .Then("search", "根据频道与身份组检索进行移除", string.Empty, RemovePermissionBySearch))
            .Then("list", "列出权限节点", "list", ListPermissions)
            .Then("check", "测试用户权限检查", "check", CheckPermission);
    }

    private async Task<(IEnumerable<GetRoleListOutput>, IEnumerable<GetChannelListOutput>)> GetBasicInfos(OpenApiService openApiService, string islandId)
    {
        var roles = await openApiService.GetRoleListAsync(new GetRoleListInput
        {
            IslandId = islandId
        }, true);
        var channels = await openApiService.GetChannelListAsync(new GetChannelListInput
        {
            IslandId = islandId
        }, true);

        return (roles, channels);
    }
}
