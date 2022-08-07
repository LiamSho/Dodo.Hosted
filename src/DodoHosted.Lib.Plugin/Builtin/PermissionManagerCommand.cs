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
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.App.Helpers;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Lib.Plugin.Builtin;

[CommandExecutor(
    commandName: "pm",
    description: "权限管理器",
    helpText: @"""
- `{{PREFIX}}pm add <权限节点> <#频道名/频道 ID/*> <身份组 ID/*> <allow/deny>`  添加权限组
- `{{PREFIX}}pm set <权限 ID> [channel <#频道名/频道 ID/*>] [role <身份组 ID/*>] [value <allow/deny>]`  更新权限信息
- `{{PREFIX}}pm check <权限节点> <#频道名/频道 ID> <@用户/用户 ID>`  检查用户权限
- `{{PREFIX}}pm list [channel <#频道名/频道 ID/*>] [role <身份组 ID/*>]`  列出权限表
- `{{PREFIX}}pm remove single <权限 ID> [--dry-run]`  移除一个权限配置
- `{{PREFIX}}pm remove nodes <权限节点> [--dry-run]`  按照权限节点匹配进行移除
- `{{PREFIX}}pm remove search <#频道名/频道 ID/*> <身份组 ID/*> [--dry-run]`  按照频道与身份组检索进行移除
""")]
public class PermissionManagerCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage message,
        IServiceProvider provider,
        Func<string, Task<string>> reply,
        bool shouldAllow = false)
    {
        var permissionManager = provider.GetRequiredService<IPermissionManager>();
        
        if (shouldAllow is false)
        {
            if (await permissionManager.CheckPermission("pm", message) is false)
            {
                return CommandExecutionResult.Unauthorized;
            }
        }

        var arg = args.Skip(1).FirstOrDefault();
        switch (arg)
        {
            case "add":
                if (args.Length != 6)
                {
                    return CommandExecutionResult.Unknown;
                }
                return await RunAddPermission(args, reply, message, permissionManager);
            case "check":
                if (args.Length != 5)
                {
                    return CommandExecutionResult.Unknown;
                }
                var openApiServices = provider.GetRequiredService<OpenApiService>();
                return await RunCheckPermission(args, reply, message, openApiServices, permissionManager);
            case "set":
                if (args.Length < 5)
                {
                    return CommandExecutionResult.Unknown;
                }
                return await RunSetPermission(args, reply, message, permissionManager);
            case "list":
                if (args.Length is not (2 or 4 or 6))
                {
                    return CommandExecutionResult.Unknown;
                }
                return await RunListPermission(args, reply, message, permissionManager);
            case "remove":
                if (args.Length < 4)
                {
                    return CommandExecutionResult.Unknown;
                }
                return await RunRemovePermission(args, reply, message, permissionManager);
            default:
                return CommandExecutionResult.Unknown;
        }
    }

    // pm add <node> <(#channel/\*)> <role(id/\*)> <allow/deny>
    private static async Task<CommandExecutionResult> RunAddPermission(
        string[] args,
        Func<string, Task<string>> reply,
        CommandMessage message,
        IPermissionManager permissionManager)
    {
        var node = args.Skip(2).FirstOrDefault();
        var channel = args.Skip(3).FirstOrDefault();
        var role = args.Skip(4).FirstOrDefault();
        var value = args.Skip(5).FirstOrDefault();

        if (node is null || channel is null || role is null || value is null)
        {
            return CommandExecutionResult.Failed;
        }

        // 若节点有 *，则只能是最后一位为 *
        if (node.Contains('*'))
        {
            if (node.EndsWith("*") is false)
            {
                await reply.Invoke("Node 不合法");
                return CommandExecutionResult.Failed; 
            }
        }
        
        // 解析 Channel
        channel = channel.ExtractChannelId(true);
        if (channel is null)
        {
            await reply.Invoke("Channel 不合法");
            return CommandExecutionResult.Failed;
        }
        
        // 检查 Role
        if (role != "*" && long.TryParse(role, out _) is false)
        {
            await reply.Invoke("Role 不合法");
            return CommandExecutionResult.Failed;
        }
        
        // 检查 Value
        if (value is not ("allow" or "deny"))
        {
            await reply.Invoke("Value 不合法");
            return CommandExecutionResult.Failed;
        }

        var result = await permissionManager.AddPermission(node, message.IslandId, channel, role, value);

        if (result is null)
        {
            await reply.Invoke("添加权限节点失败，可能已经存在值相同的同名节点");
            return CommandExecutionResult.Failed;
        }

        await reply.Invoke($"权限节点添加成功：`{result}`");
        return CommandExecutionResult.Success;
    }
    
    // pm set <权限 ID> [channel <#频道名/频道 ID/*>] [role <身份组 ID/*>] [value <allow/deny>]
    private static async Task<CommandExecutionResult> RunSetPermission(
        string[] args,
        Func<string, Task<string>> reply,
        CommandMessage message,
        IPermissionManager permissionManager)
    {
        var id = args.Skip(2).FirstOrDefault();
        var guidParsed = Guid.TryParse(id, out var parsedGuid);
        if (guidParsed is false)
        {
            await reply.Invoke("Guid 不合法");
            return CommandExecutionResult.Failed;
        }
        
        var skippedArgs = args.Skip(3).ToList();

        string? channel = null;
        string? role = null;
        string? value = null;
        
        while (skippedArgs.Count != 0)
        {
            switch (skippedArgs.FirstOrDefault())
            {
                case "channel":
                    skippedArgs.RemoveAt(0);
                    channel = skippedArgs.FirstOrDefault();
                    if (channel is null)
                    {
                        return CommandExecutionResult.Unknown;
                    }
                    skippedArgs.RemoveAt(0);
                    break;
                case "role":
                    skippedArgs.RemoveAt(0);
                    role = skippedArgs.FirstOrDefault();
                    if (role is null)
                    {
                        return CommandExecutionResult.Unknown;
                    }
                    skippedArgs.RemoveAt(0);
                    break;
                case "value":
                    skippedArgs.RemoveAt(0);
                    value = skippedArgs.FirstOrDefault();
                    if (value is null)
                    {
                        return CommandExecutionResult.Unknown;
                    }
                    skippedArgs.RemoveAt(0);
                    break;
                default:
                    skippedArgs.Clear();
                    break;
            }
        }
        
        if (channel is not null)
        {
            channel = channel.ExtractChannelId(true);
            if (channel is null)
            {
                await reply.Invoke("Channel 不合法");
                return CommandExecutionResult.Failed;
            }
        }

        if (role is not null)
        {
            if (role != "*" && long.TryParse(role, out _) is false)
            {
                await reply.Invoke("Role 不合法");
                return CommandExecutionResult.Failed;
            }  
        }

        if (value is not null)
        {
            if (value is not ("allow" or "deny"))
            {
                await reply.Invoke("Value 不合法");
                return CommandExecutionResult.Failed;
            }
        }

        var (ori, upt) = await permissionManager
            .SetPermissionSchema(message.IslandId, parsedGuid, channel, role, value);

        if (ori is null)
        {
            await reply.Invoke($"找不到 ID 为 `{id}` 的记录");
            return CommandExecutionResult.Failed;
        }

        await reply.Invoke($"修改成功：\n- 初始值：`{ori}`\n- 修改后：`{upt}`");
        return CommandExecutionResult.Success;
    }

    // pm check <node> <#channel> <(@member/id)>
    private static async Task<CommandExecutionResult> RunCheckPermission(
        string[] args,
        Func<string, Task<string>> reply,
        CommandMessage message,
        OpenApiService openApiService,
        IPermissionManager permissionManager)
    {
        var node = args.Skip(2).FirstOrDefault();
        var channel = args.Skip(3).FirstOrDefault();
        var member = args.Skip(4).FirstOrDefault();
        
        if (node is null || channel is null || member is null)
        {
            return CommandExecutionResult.Failed;
        }

        // 检查 Node
        if (node.Contains('*'))
        {
            await reply.Invoke("Node 不可包含 `*`");
            return CommandExecutionResult.Failed;
        }
        
        // 解析 Channel
        channel = channel.ExtractChannelId(true);
        if (channel is null)
        {
            await reply.Invoke("Channel 不合法");
            return CommandExecutionResult.Failed;
        }
        
        // 解析 Member
        member = member.ExtractMemberId();
        if (member is null)
        {
            await reply.Invoke("用户 ID 不合法");
            return CommandExecutionResult.Failed;
        }

        var memberRoles = await openApiService.GetMemberRoleListAsync(new GetMemberRoleListInput
        {
            IslandId = message.IslandId, DodoId = member
        });

        if (memberRoles is null)
        {
            await reply.Invoke("获取用户身份组列表失败");
            return CommandExecutionResult.Failed;
        }

        var result = await permissionManager.DescribeSchemaCheck(node, memberRoles, message.IslandId, channel);

        var resultString = result?.Value == "allow" ? "Allow" : "Deny";

        var __ = result is null
            ? await reply.Invoke($"权限检查结果：`{resultString}`\n匹配规则：`NULL`")
            : await reply.Invoke($"权限检查结果：`{resultString}`\n匹配规则：`{result}`");
        
        return CommandExecutionResult.Success;
    }

    // pm list [channel <#频道名/频道 ID/*>] [role <身份组 ID/*>]
    private static async Task<CommandExecutionResult> RunListPermission(
        IEnumerable<string> args,
        Func<string, Task<string>> reply,
        CommandMessage message,
        IPermissionManager permissionManager)
    {
        var skippedArgs = args.Skip(2).ToList();
        string? channel = null;
        string? role = null;

        while (skippedArgs.Count != 0)
        {
            switch (skippedArgs.FirstOrDefault())
            {
                case "channel":
                    skippedArgs.RemoveAt(0);
                    channel = skippedArgs.FirstOrDefault();
                    if (channel is null)
                    {
                        return CommandExecutionResult.Unknown;
                    }
                    skippedArgs.RemoveAt(0);
                    break;
                case "role":
                    skippedArgs.RemoveAt(0);
                    role = skippedArgs.FirstOrDefault();
                    if (role is null)
                    {
                        return CommandExecutionResult.Unknown;
                    }
                    skippedArgs.RemoveAt(0);
                    break;
                default:
                    skippedArgs.Clear();
                    break;
            }
        }

        if (channel is not null)
        {
            channel = channel.ExtractChannelId(true);
            if (channel is null)
            {
                await reply.Invoke("Channel 不合法");
                return CommandExecutionResult.Failed;
            }
        }

        if (role is not null)
        {
            if (role != "*" && long.TryParse(role, out _) is false)
            {
                await reply.Invoke("Role 不合法");
                return CommandExecutionResult.Failed;
            }  
        }

        var perms = await permissionManager
            .GetPermissionSchemas(message.IslandId, channel, role);

        if (perms.Count == 0)
        {
            await reply.Invoke("找不到符合检索条件的权限组");
            return CommandExecutionResult.Success;
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

        await reply.Invoke(messageBuilder.ToString());

        return CommandExecutionResult.Success;
    }
    
    // pm remove single <权限 ID> [--dry-run]`  移除一个权限配置
    // pm remove nodes <权限节点> [--dry-run]`  按照权限节点匹配进行移除
    // pm remove search <#频道名/频道 ID/*> <身份组 ID/*> [--dry-run]`  按照频道与身份组检索进行移除
    private static async Task<CommandExecutionResult> RunRemovePermission(
        IReadOnlyList<string> args,
        Func<string, Task<string>> reply,
        CommandMessage message,
        IPermissionManager permissionManager)
    {
        var dryRun = args.Contains("--dry-run");
        var type = args[2];

        var messageBuilder = new StringBuilder();
        messageBuilder.AppendLine(dryRun ? "`DRY-RUN` 将删除以下权限记录：" : "以下权限记录已删除：");

        var removed = new List<PermissionSchema>();
        
        switch (type)
        {
            case "single":
                var singleGuid = args.Skip(3).FirstOrDefault(x => x.StartsWith("--") is false);
                var guidParsed = Guid.TryParse(singleGuid, out var parsedGuid);
                if (guidParsed is false)
                {
                    await reply.Invoke("Guid 不合法");
                    return CommandExecutionResult.Failed;
                }
                var singleResult = await permissionManager.RemovePermissionSchemaById(message.IslandId, parsedGuid, dryRun);
                if (singleResult is not null)
                {
                    removed.Add(singleResult);
                }
                break;
            case "nodes":
                var node = args.Skip(3).FirstOrDefault(x => x.StartsWith("--") is false);
                if (node is null)
                {
                    await reply.Invoke("Node 不合法");
                    return CommandExecutionResult.Failed;
                }
                removed = await permissionManager.RemovePermissionSchemasByNode(message.IslandId, node, dryRun);
                break;
            case "search":
                var channel = args.Skip(3).FirstOrDefault(x => x.StartsWith("--") is false);
                var role = args.Skip(4).FirstOrDefault(x => x.StartsWith("--") is false);
                if (role is null || channel is null)
                {
                    return CommandExecutionResult.Unknown;
                }

                channel = channel.ExtractChannelId(true);

                if (channel is null)
                {
                    await reply.Invoke("Channel 不合法");
                    return CommandExecutionResult.Failed;
                }
                if (role != "*" && long.TryParse(role, out _) is false)
                {
                    await reply.Invoke("Role 不合法");
                    return CommandExecutionResult.Failed;
                }

                removed = await permissionManager.RemovePermissionSchemasBySearch(message.IslandId, channel, role, dryRun);
                break;
            default:
                return CommandExecutionResult.Unknown;
        }

        if (removed.Count == 0)
        {
            messageBuilder.AppendLine("- `NULL`");
        }
        else
        {
            var lines = removed.Select(x => $"- `{x}`");
            messageBuilder.AppendJoin('\n', lines);
        }
        
        await reply.Invoke(messageBuilder.ToString());
        
        return CommandExecutionResult.Success;
    }
}
