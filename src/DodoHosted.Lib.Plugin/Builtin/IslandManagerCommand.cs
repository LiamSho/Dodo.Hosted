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
        if (shouldAllow is false)
        {
            if (await permissionManager.CheckPermission("system.command.island", message) is false)
            {
                return CommandExecutionResult.Unauthorized;
            }
        }
        
        // island <get/set> <param> [value]
        // island send <#频道名/频道 ID> <text/image/video> <消息体>
        // 至少 3 位
        if (args.Length < 3)
        {
            return CommandExecutionResult.Unknown;
        }

        var islandInfoCollection = provider
            .GetRequiredService<IMongoDatabase>()
            .GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);
        var openApi = provider.GetRequiredService<OpenApiService>();

        switch (args[1])
        {
            case "set":
                return await RunSetParams(args, islandInfoCollection, reply, message);
            case "get":
                return await RunGetInfos(args, islandInfoCollection, openApi, reply, message);
            case "send":
                var channelId = args.Skip(2).Take(1).FirstOrDefault()?.ExtractChannelId();
                var sendMessage = args.Skip(3).Take(1).FirstOrDefault();
                if (string.IsNullOrEmpty(channelId) || string.IsNullOrEmpty(sendMessage))
                {
                    return CommandExecutionResult.Unknown;
                }

                await openApi.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyText>
                {
                    ChannelId = channelId, MessageBody = new MessageBodyText { Content = sendMessage }
                });

                await reply("已发送");
                return CommandExecutionResult.Success;

            default:
                return CommandExecutionResult.Unknown;
        }
    }

    public CommandMetadata GetMetadata() => new CommandMetadata(
        CommandName: "island",
        Description: "群组配置",
        HelpText: @"""
- `{{PREFIX}}island set logger_channel <#频道名/频道 ID>`    设置日志频道
- `{{PREFIX}}island set logger_channel_enabled <true/false>`    设置日志频道是否启用
- `{{PREFIX}}island get settings`    获取群组配置信息
- `{{PREFIX}}island get roles`    获取群组身份组信息
- `{{PREFIX}}island send <#频道名/频道 ID> <消息体>`    在频道中发送消息
""",
        PermissionNodes: new Dictionary<string, string>
        {
            { "system.command.island", "允许使用 `island` 指令" }
        });

    private static async Task<CommandExecutionResult> RunSetParams(
        string[] args,
        IMongoCollection<IslandSettings> collection,
        Func<string, Task<string>> reply,
        CommandMessage message)
    {
        var param = args.Skip(2).FirstOrDefault();
        var value = args.Skip(3).FirstOrDefault();
        
        if (param is null || value is null)
        {
            return CommandExecutionResult.Unknown;
        }

        switch (param)
        {
            case "logger_channel":
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
            default:
                return CommandExecutionResult.Unknown;
        }
    }
    
    private static async Task<CommandExecutionResult> RunGetInfos(
        IEnumerable<string> args,
        IMongoCollection<IslandSettings> collection,
        OpenApiService openApiService,
        Func<string, Task<string>> reply,
        CommandMessage message)
    {
        var infoType = args.Skip(2).FirstOrDefault();
        
        if (infoType is null)
        {
            return CommandExecutionResult.Unknown;
        }

        switch (infoType)
        {
            case "settings":
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
                
                foreach (var role in roles.OrderBy(x => x.Position))
                {
                    roleMessageBuilder.AppendLine($"- {FormatLength(role.Position.ToString(), positionMaxLength, '0')} " +
                                                  $"`{FormatLength(role.RoleId, idMaxLength)}` " +
                                                  $"`{role.RoleName}`");
                }
                
                await reply(roleMessageBuilder.ToString());
                
                return CommandExecutionResult.Success;
            default:
                return CommandExecutionResult.Unknown;
        }
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
