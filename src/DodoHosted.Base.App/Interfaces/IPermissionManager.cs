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
using DodoHosted.Base.App.Entities;

namespace DodoHosted.Base.App.Interfaces;

public interface IPermissionManager
{
    Task<PermissionSchema?> DescribeSchemaCheck(string node, IEnumerable<GetMemberRoleListOutput> roles, string islandId, string channelId);
    Task<bool> CheckPermission(string node, IEnumerable<GetMemberRoleListOutput> roles, string islandId, string channelId);
    Task<PermissionSchema?> AddPermission(string node, string islandId, string channelId, string roleId, string value);
    Task<(PermissionSchema?, PermissionSchema?)> SetPermissionSchema(string islandId, Guid id, string? channel = null, string? role = null, string? value = null);
    Task<List<PermissionSchema>> GetPermissionSchemas(string islandId, string? channelId = null, string? roleId = null);
    Task<PermissionSchema?> RemovePermissionSchemaById(string islandId, Guid id, bool dryRun = false);
    Task<List<PermissionSchema>> RemovePermissionSchemasByNode(string islandId, string node, bool dryRun = false);
    Task<List<PermissionSchema>> RemovePermissionSchemasBySearch(string islandId, string? channel = null, string? role = null, bool dryRun = false);
}
