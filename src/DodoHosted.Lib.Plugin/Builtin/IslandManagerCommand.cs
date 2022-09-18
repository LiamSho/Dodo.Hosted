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
using DoDo.Open.Sdk.Models.Resources;
using DoDo.Open.Sdk.Models.Roles;
using MongoDB.Driver;

namespace DodoHosted.Lib.Plugin.Builtin;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

public sealed class IslandManagerCommand : ICommandExecutor
{
    public async Task<bool> SetLoggerChannel(
        PluginBase.Context context,
        [CmdInject] IMongoCollection<IslandSettings> collection,
        [CmdOption("channel", "c", "日志频道")] DodoChannelId channel)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            await context.Functions.Reply.Invoke("修改失败，找不到记录");
            return false;
        }

        settings.LoggerChannelId = channel.Value;
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Functions.Reply.Invoke("修改成功");
        return true;
    }
    
    public async Task<bool> EnableLoggerChannel(
        PluginBase.Context context,
        [CmdInject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            await context.Functions.Reply.Invoke("修改失败，找不到记录");
            return false;
        }

        settings.EnableChannelLogger = true;
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Functions.Reply.Invoke("修改成功");
        return true;
    }
    
    public async Task<bool> DisableLoggerChannel(
        PluginBase.Context context,
        [CmdInject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            await context.Functions.Reply.Invoke("修改失败，找不到记录");
            return false;
        }

        settings.EnableChannelLogger = false;
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Functions.Reply.Invoke("修改成功");
        return true;
    }

    public async Task<bool> RenewWebApiToken(
        PluginBase.Context context,
        [CmdInject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            await context.Functions.Reply.Invoke("修改失败，找不到记录");
            return false;
        }

        settings.WebApiToken = TokenHelper.GenerateToken();
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Functions.Reply.Invoke("修改成功");
        return true;
    }

    public async Task<bool> GetSettings(
        PluginBase.Context context,
        [CmdInject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        var logChannel = string.IsNullOrEmpty(settings.LoggerChannelId)
                     ? "`NULL`"
                     : $"<#{settings.LoggerChannelId}> `ID: {settings.LoggerChannelId}`";
        
        var islandSettingMessageBuilder = new StringBuilder();
        islandSettingMessageBuilder.AppendLine("群组配置信息：");
        islandSettingMessageBuilder.AppendLine($"- 启用日志频道：`{settings.EnableChannelLogger}`");
        islandSettingMessageBuilder.AppendLine($"- 日志频道：{logChannel}");
        
        await context.Functions.Reply.Invoke(islandSettingMessageBuilder.ToString());
        return true;
    }

    public async Task<bool> GetRoles(PluginBase.Context context)
    {
        var roles = await context.OpenApiService
            .GetRoleListAsync(new GetRoleListInput { IslandId = context.EventInfo.IslandId }, true);
        if (roles is null)
        {
            await context.Functions.Reply.Invoke("找不到群组身份组信息");
            return false;
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
                 
        await context.Functions.Reply.Invoke(roleMessageBuilder.ToString());

        return true;
    }

    public async Task<bool> GetWebApiToken(
        PluginBase.Context context,
        [CmdInject] IMongoCollection<IslandSettings> collection)
    {
        var token = collection.AsQueryable()
            .FirstOrDefault(x => x.IslandId == context.EventInfo.IslandId)?.WebApiToken;

        if (token is null)
        {
            await context.Functions.Reply.Invoke("找不到记录");
            return false;
        }
                 
        await context.Functions.Reply.Invoke($"Web API Token: `{token}`", true);
        return true;
    }

    public async Task<bool> SendText(PluginBase.Context context,
        [CmdOption("channel", "c", "发送目标频道")] DodoChannelId channel,
        [CmdOption("message", "m", "要发送的消息")] string message,
        [CmdOption("user", "u", "发送只有某个用户看到的的消息", false)] DodoMemberId? user,
        [CmdOption("reply", "r", "回复消息 ID", false)] string? replyId)
    {
        await context.OpenApiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyText>
        {
            ChannelId = channel.Value,
            MessageBody = new MessageBodyText
            {
                Content = message
            },
            DodoId = user is null ? null :
                user.Value.Valid ? user.Value.Value : null,
            ReferencedMessageId = replyId
        }, true);
        
        return true;
    }
    
    public async Task<bool> SendImage(PluginBase.Context context,
        [CmdOption("channel", "c", "发送目标频道")] DodoChannelId channel,
        [CmdOption("image", "i", "要发送的图片链接")] string url,
        [CmdOption("user", "u", "发送只有某个用户看到的的消息", false)] DodoMemberId? user,
        [CmdOption("reply", "r", "回复消息 ID", false)] string? replyId)
    {
        var response = await context.OpenApiService
            .SetResourcePictureUploadAsync(new SetResourceUploadInput { FilePath = url }, true);
        
        await context.OpenApiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyPicture>
        {
            ChannelId = channel.Value,
            MessageBody = new MessageBodyPicture
            {
                Url = response.Url,
                Width = response.Height,
                Height = response.Height,
                IsOriginal = 1
            },
            DodoId = user is null ? null :
                user.Value.Valid ? user.Value.Value : null,
            ReferencedMessageId = replyId
        }, true);
        
        return true;
    }

    public async Task<bool> SendReaction(PluginBase.Context context,
        [CmdOption("id", "i", "消息 ID")] string msgId,
        [CmdOption("emoji", "e", "反应 Emoji")] DodoEmoji emoji)
    {
        await context.OpenApiService.SetChannelMessageReactionAddAsync(new SetChannelMessageReactionAddInput
        {
            MessageId = msgId, Emoji = new MessageModelEmoji { Id = emoji.EmojiId.ToString(), Type = 1 }
        }, true);

        return true;
    }
    
    public async Task<bool> DeleteMessage(PluginBase.Context context,
        [CmdOption("id", "i", "消息 ID")] string msgId,
        [CmdOption("reason", "r", "删除原因", false)] string? reason)
    {
        await context.OpenApiService.SetChannelMessageWithdrawAsync(new SetChannelMessageWithdrawInput
        {
            MessageId = msgId, Reason = reason
        }, true);

        return true;
    }
    
    public async Task<bool> DeleteReaction(PluginBase.Context context,
        [CmdOption("id", "i", "消息 ID")] string msgId,
        [CmdOption("emoji", "e", "反应 Emoji")] DodoEmoji emoji)
    {
        await context.OpenApiService.SetChannelMessageReactionRemoveAsync(new SetChannelMessageReactionRemoveInput
        {
            MessageId = msgId, Emoji = new MessageModelEmoji { Id = emoji.EmojiId.ToString(), Type = 1 }
        }, true);

        return true;
    }

    public CommandTreeBuilder GetBuilder()
    {
        return new CommandTreeBuilder("island", "群组管理", "system.island")
            .Then("logger", "群组日志记录器", "settings", builder: x => x
                .Then("channel", "设置日志频道", "modify", SetLoggerChannel)
                .Then("enable", "设置日志频道", "modify", EnableLoggerChannel)
                .Then("disable", "设置日志频道", "modify", DisableLoggerChannel))
            .Then("web", "群组 WebAPI 配置", "web", builder: x => x
                .Then("renew", "更新 WebAPI Token", string.Empty, RenewWebApiToken))
            .Then("get", "获取群组信息", "settings.read", builder: x => x
                .Then("settings", "获取群组信息", "config", GetSettings)
                .Then("roles", "获取群组角色信息", "roles", GetRoles)
                .Then("token", "获取群组 WebAPI Token", "web", GetWebApiToken))
            .Then("send", "发送消息", "message.send", builder: x => x
                .Then("text", "发送文字消息", "text", SendText)
                .Then("image", "发送图片消息", "image", SendImage)
                .Then("reaction", "发送消息反应", "reaction", SendReaction))
            .Then("delete", "删除消息", "message.delete", builder: x => x
                .Then("message", "删除消息", "message", DeleteMessage)
                .Then("reaction", "删除消息反应", "reaction", DeleteReaction));
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
