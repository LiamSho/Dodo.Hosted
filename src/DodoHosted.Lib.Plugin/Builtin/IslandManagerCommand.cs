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
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Models.Roles;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Entities;
using DodoHosted.Base.App.Helpers;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace DodoHosted.Lib.Plugin.Builtin;

public class IslandManagerCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage message,
        IServiceProvider provider,
        IPermissionManager permissionManager,
        Func<string, Task<string>> reply,
        bool shouldAllow = false)
    {
        Func<string, Task<bool>> permCheck = async s =>
        {
            if (shouldAllow)
            {
                return true;
            }

            return await permissionManager.CheckPermission(s, message);
        };
        
        var islandInfoCollection = provider
            .GetRequiredService<IMongoDatabase>()
            .GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);
        var openApi = provider.GetRequiredService<OpenApiService>();

        return args switch
        {
            [_, "set", var param, var value] => await RunSetParams(param, value, islandInfoCollection, reply, permCheck, message),
            [_, "get", var param] => await RunGetInfos(param, islandInfoCollection, openApi, reply, permCheck, message),
            [_, "send", var channel, var content] => await RunSendMessage(channel, content, openApi, reply, permCheck),
            _ => CommandExecutionResult.Unknown
        };
    }

    public CommandMetadata GetMetadata() => new CommandMetadata(
        CommandName: "island",
        Description: "群组配置",
        HelpText: @"""
- `{{PREFIX}}island set logger_channel <#频道名/频道 ID>`    设置日志频道
- `{{PREFIX}}island set logger_channel_enabled <true/false>`    设置日志频道是否启用
- `{{PREFIX}}island set web_api_token new`    生成新的 Web API Token
- `{{PREFIX}}island get settings`    获取群组配置信息
- `{{PREFIX}}island get roles`    获取群组身份组信息
- `{{PREFIX}}island get web_api_token`    获取群组 Web API Token
- `{{PREFIX}}island send <#频道名/频道 ID> <消息体>`    在频道中发送消息
""",
        PermissionNodes: new Dictionary<string, string>
        {
            { "system.island.roles", "允许使用 `island get role` 指令" },
            { "system.island.settings", "允许查看或设置群组配置" },
            { "system.island.web", "允许 Web Token 相关操作" },
            { "system.island.message", "允许发送消息操作" }
        });

    private static async Task<CommandExecutionResult> RunSetParams(
        string param,
        string value,
        IMongoCollection<IslandSettings> collection,
        Func<string, Task<string>> reply,
        Func<string, Task<bool>> permCheck,
        CommandMessage message)
    {
        switch (param)
        {
            case "logger_channel":
                if (await permCheck.Invoke("system.island.settings") is false)
                {
                    return CommandExecutionResult.Unauthorized;
                }
                
                var channelId = value.ExtractChannelId();
                if (channelId is null)
                {
                    await reply("频道 ID 无效");
                    return CommandExecutionResult.Failed;
                }

                var info1 = collection.FindOneAndUpdate(x => x.IslandId == message.IslandId,
                    Builders<IslandSettings>.Update.Set(x => x.LoggerChannelId, channelId));

                if (info1 is null)
                {
                    await reply("修改失败，找不到记录");
                }
                
                await reply("修改成功");
                return CommandExecutionResult.Success;

            case "logger_channel_enabled":
                if (await permCheck.Invoke("system.island.settings") is false)
                {
                    return CommandExecutionResult.Unauthorized;
                }
                
                if (value is not ("true" or "false"))
                {
                    await reply("参数无效");
                    return CommandExecutionResult.Failed;
                }

                var enabled = value is "true";
                var info2 = collection.FindOneAndUpdate(x => x.IslandId == message.IslandId,
                    Builders<IslandSettings>.Update.Set(x => x.EnableChannelLogger, enabled));
                
                if (info2 is null)
                {
                    await reply("修改失败，找不到记录");
                }
                
                await reply("修改成功");
                
                return CommandExecutionResult.Success;
            case "web_api_token":
                if (await permCheck.Invoke("system.island.web") is false)
                {
                    return CommandExecutionResult.Unauthorized;
                }
                
                if (value is not "new")
                {
                    await reply("参数无效");
                    return CommandExecutionResult.Failed;
                }

                var info3 = collection.FindOneAndUpdate(x => x.IslandId == message.IslandId,
                    Builders<IslandSettings>.Update.Set(x => x.WebApiToken, TokenHelper.GenerateToken()));
                
                if (info3 is null)
                {
                    await reply("修改失败，找不到记录");
                }
                
                await reply("修改成功");
                
                return CommandExecutionResult.Success;
            default:
                return CommandExecutionResult.Unknown;
        }
    }
    
    private static async Task<CommandExecutionResult> RunGetInfos(
        string infoType,
        IMongoCollection<IslandSettings> collection,
        OpenApiService openApiService,
        Func<string, Task<string>> reply,
        Func<string, Task<bool>> permCheck,
        CommandMessage message)
    {
        switch (infoType)
        {
            case "settings":
                if (await permCheck.Invoke("system.island.settings") is false)
                {
                    return CommandExecutionResult.Unauthorized;
                }
                
                var islandSettings = collection.AsQueryable().FirstOrDefault(x => x.IslandId == message.IslandId);

                if (islandSettings is null)
                {
                    await reply("找不到群组配置信息");
                    return CommandExecutionResult.Failed;
                }

                var logChannel = string.IsNullOrEmpty(islandSettings.LoggerChannelId)
                    ? "`NULL`"
                    : $"<#{islandSettings.LoggerChannelId}> `ID: {islandSettings.LoggerChannelId}`";

                var islandSettingMessageBuilder = new StringBuilder();
                islandSettingMessageBuilder.AppendLine("群组配置信息：");
                islandSettingMessageBuilder.AppendLine($"- 启用日志频道：`{islandSettings.EnableChannelLogger}`");
                islandSettingMessageBuilder.AppendLine($"- 日志频道：{logChannel}");

                await reply(islandSettingMessageBuilder.ToString());
                
                return CommandExecutionResult.Success;

            case "roles":
                if (await permCheck.Invoke("system.island.roles") is false)
                {
                    return CommandExecutionResult.Unauthorized;
                }

                var roles = await openApiService.GetRoleListAsync(new GetRoleListInput { IslandId = message.IslandId });
                if (roles is null)
                {
                    await reply("找不到群组身份组信息");
                    return CommandExecutionResult.Failed;
                }
                
                var roleMessageBuilder = new StringBuilder();
                roleMessageBuilder.AppendLine("群组身份组信息：");

                var idMaxLength = roles.Select(x => x.RoleId.Length).Max();
                var positionMaxLength = roles.Select(x => x.Position.ToString().Length).Max();
                var maxPosition = roles.Select(x => x.Position).Max();
                
                foreach (var role in roles.OrderByDescending(x => x.Position))
                {
                    var descPosition = maxPosition - role.Position + 1;
                    roleMessageBuilder.AppendLine($"- {FormatLength(descPosition.ToString(), positionMaxLength, '0')} " +
                                                  $"`{FormatLength(role.RoleId, idMaxLength)}` " +
                                                  $"{FormatLength(role.RoleColor, 7)} " +
                                                  $"`{role.RoleName}`");
                }
                
                await reply(roleMessageBuilder.ToString());
                
                return CommandExecutionResult.Success;
            case "web_api_token":
                if (await permCheck.Invoke("system.island.web") is false)
                {
                    return CommandExecutionResult.Unauthorized;
                }

                var token = collection.AsQueryable().FirstOrDefault(x => x.IslandId == message.IslandId)?.WebApiToken;

                if (token is null)
                {
                    await reply.Invoke("找不到记录");
                    return CommandExecutionResult.Failed;
                }
                
                await reply.Invoke($"Web API Token: `{token}`");
                
                return CommandExecutionResult.Success;
            default:
                return CommandExecutionResult.Unknown;
        }
    }

    private static async Task<CommandExecutionResult> RunSendMessage(
        string channel,
        string content,
        OpenApiService openApi,
        Func<string, Task<string>> reply,
        Func<string, Task<bool>> permCheck)
    {
        if (await permCheck.Invoke("system.island.message") is false)
        {
            return CommandExecutionResult.Unauthorized;
        }
        
        var channelId = channel.ExtractChannelId();
        if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(content))
        {
            return CommandExecutionResult.Unknown;
        }

        await openApi.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyText>
        {
            ChannelId = channelId, MessageBody = new MessageBodyText { Content = content }
        });

        await reply("已发送");
        return CommandExecutionResult.Success;
    }

    private static string FormatLength(string str, int maxLength, char placeHolder = ' ')
    {
        var s = str;
        while (s.Length < maxLength)
        {
            s = placeHolder + s;
        }

        return s;
    }
}
