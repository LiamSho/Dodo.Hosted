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

using System.Text;
using DoDo.Open.Sdk.Models.Members;
using DodoHosted.Base;
using DodoHosted.Base.App.Command.Attributes;
using DodoHosted.Base.App.Command.Builder;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Types;
using DodoHosted.Open.Plugin;

namespace DodoHosted.Lib.Plugin.Builtin;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

public class PermissionManagerCommand : ICommandExecutor
{
    public async Task<bool> AddPermission(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
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
                await context.Functions.Reply.Invoke("Node 不合法");
                return false;
            }
        }
         
        // 检查 Role
        if (roleId != "*" && long.TryParse(roleId, out _) is false)
        {
            await context.Functions.Reply.Invoke("Role 不合法");
            return false;
        }
         
        // 检查 Value
        if (value is not ("allow" or "deny"))
        { 
            await context.Functions.Reply.Invoke("Value 不合法");
            return false;
        }

        var result = await pm.AddPermission(node, context.EventInfo.IslandId, channelId.Value, roleId, value);

        if (result is null)
        {
            await context.Functions.Reply.Invoke("添加权限节点失败，可能已经存在值相同的同名节点");
            return false;
        }

        await context.Functions.Reply.Invoke($"权限节点添加成功：`{result}`");
        return true;
    }

    public async Task<bool> SetPermission(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
        [CmdOption("id", "i", "权限节点的 GUID")] string nodeId,
        [CmdOption("channel", "c", "适用频道", false)] DodoChannelIdWithWildcard? channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`", false)] string? roleId,
        [CmdOption("value", "v", "值，可为 `allow` 或 `deny`", false)] string? value)
    {
        var guidParsed = Guid.TryParse(nodeId, out var parsedGuid);
        if (guidParsed is false)
        {
            await context.Functions.Reply.Invoke("Guid 不合法");
            return false;
        }
        
        if (roleId is not null)
        {
            if (roleId != "*" && long.TryParse(roleId, out _) is false)
            {
                await context.Functions.Reply.Invoke("Role 不合法");
                return false;
            }  
        }

        if (value is not null)
        {
            if (value is not ("allow" or "deny"))
            {
                await context.Functions.Reply.Invoke("Value 不合法");
                return false;
            }
        }

        var (ori, upt) = await pm
            .SetPermissionSchema(context.EventInfo.IslandId, parsedGuid, channelId?.Value, roleId, value);

        if (ori is null)
        {
            await context.Functions.Reply.Invoke($"找不到 ID 为 `{nodeId}` 的记录");
            return false;
        }

        await context.Functions.Reply.Invoke($"修改成功：\n- 初始值：`{ori}`\n- 修改后：`{upt}`");
        return true;
    }

    public async Task<bool> RemovePermissionById(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
        [CmdOption("id", "i", "权限节点的 GUID")] string nodeId,
        [CmdOption("dry-run", "d", "Dry Run", false)] bool? isDryRun)
    {
        var dryRun = isDryRun ?? false;
        
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine(dryRun ? "`DRY-RUN` 将删除以下权限记录：" : "以下权限记录已删除：");

        var guidParsed = Guid.TryParse(nodeId, out var parsedGuid);
        if (guidParsed is false)
        {
            await context.Functions.Reply.Invoke("Guid 不合法");
            return false;
        }
        
        var removePermissionSchema = await pm.RemovePermissionSchemaById(context.EventInfo.IslandId, parsedGuid, dryRun);
        if (removePermissionSchema is not null)
        {
            messageBuilder.AppendLine("- `NULL`");
        }
        else
        {
            messageBuilder.AppendLine($"- {removePermissionSchema}");
        }

        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        return true;
    }

    public async Task<bool> RemovePermissionByNode(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
        [CmdOption("node", "n", "权限节点")] string node,
        [CmdOption("dry-run", "d", "Dry Run", false)] bool? isDryRun)
    {
        var dryRun = isDryRun ?? false;
        
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine(dryRun ? "`DRY-RUN` 将删除以下权限记录：" : "以下权限记录已删除：");

        var removePermissionSchema = await pm.RemovePermissionSchemasByNode(context.EventInfo.IslandId, node, dryRun);
        if (removePermissionSchema.Count == 0)
        {
            messageBuilder.AppendLine("- `NULL`");
        }
        else
        {
            messageBuilder.AppendLine($"- {removePermissionSchema}");
        }

        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        return true;
    }
    
    public async Task<bool> RemovePermissionBySearch(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
        [CmdOption("channel", "c", "适用频道")] DodoChannelIdWithWildcard channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`")] string roleId,
        [CmdOption("dry-run", "d", "Dry Run", false)] bool? isDryRun)
    {
        var dryRun = isDryRun ?? false;
        
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine(dryRun ? "`DRY-RUN` 将删除以下权限记录：" : "以下权限记录已删除：");

        var removePermissionSchema = await pm.RemovePermissionSchemasBySearch(context.EventInfo.IslandId, channelId.Value, roleId, dryRun);
        if (removePermissionSchema.Count == 0)
        {
            messageBuilder.AppendLine("- `NULL`");
        }
        else
        {
            messageBuilder.AppendLine($"- {removePermissionSchema}");
        }

        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        return true;
    }

    public async Task<bool> ListPermissions(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
        [CmdOption("channel", "c", "适用频道", false)] DodoChannelIdWithWildcard? channelId,
        [CmdOption("role", "r", "权限组 ID，可为 `*`", false)] string? roleId)
    {
        if (roleId is not null)
        {
            if (roleId != "*" && long.TryParse(roleId, out _) is false)
            {
                await context.Functions.Reply.Invoke("Role 不合法");
                return false;
            }
        }
        
        var perms = await pm.GetPermissionSchemas(context.EventInfo.IslandId, channelId?.Value, roleId);

        if (perms.Count == 0)
        {
            await context.Functions.Reply.Invoke("找不到符合检索条件的权限组");
            return true;
        }
        
        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine("检索到的权限组：");

        foreach (var perm in perms)
        {
            messageBuilder.AppendLine($"- Id: `{perm.Id}`，" +
                                      $"Node: `{perm.Node}`，" +
                                      $"Channel: `{perm.Channel}`，" +
                                      $"Role: `{perm.Role}`，" +
                                      $"Value: `{perm.Value}`");
        }

        await context.Functions.Reply.Invoke(messageBuilder.ToString());

        return true;
    }

    public async Task<bool> CheckPermission(
        PluginBase.Context context,
        [CmdInject] IPermissionManager pm,
        [CmdOption("node", "n", "权限节点")] string node,
        [CmdOption("channel", "c", "适用频道")] DodoChannelId channelId, 
        [CmdOption("user", "u", "用户")] DodoMemberId memberId)
    {
        // 检查 Node
        if (node.Contains('*'))
        {
            await context.Functions.Reply.Invoke("Node 不可包含 `*`");
            return false;
        }
        
        var memberRoles = await context.OpenApiService.GetMemberRoleListAsync(new GetMemberRoleListInput
        {
            IslandId = context.EventInfo.IslandId, DodoId = memberId.Value
        });
        
        if (memberRoles is null)
        {
            await context.Functions.Reply.Invoke("获取用户身份组列表失败");
            return false;
        }
        
        var result = await pm.DescribeSchemaCheck(node, memberRoles, context.EventInfo.IslandId, channelId.Value);

        var resultString = result?.Value == "allow" ? "Allow" : "Deny";

        var __ = result is null
            ? await context.Functions.Reply.Invoke($"权限检查结果：`{resultString}`\n匹配规则：`NULL`")
            : await context.Functions.Reply.Invoke($"权限检查结果：`{resultString}`\n匹配规则：`{result}`");

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
}
