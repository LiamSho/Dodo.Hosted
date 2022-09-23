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
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Models.Resources;
using DoDo.Open.Sdk.Models.Roles;
using DodoHosted.Base.App.Attributes;
using DodoHosted.Base.App.Context;
using DodoHosted.Base.Context;
using DodoHosted.Lib.Plugin.Cards;
using MongoDB.Driver;

namespace DodoHosted.Lib.Plugin.Builtin;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

public sealed class IslandManagerCommand : ICommandExecutor
{
    public async Task<bool> SetLoggerChannel(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection,
        [CmdOption("channel", "c", "日志频道")] DodoChannelId channel)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            throw new InternalProcessException(
                nameof(IslandManagerCommand),
                nameof(SetLoggerChannel),
                $"找不到频道 {context.EventInfo.IslandId} 的配置记录");
        }

        settings.LoggerChannelId = channel.Value;
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Reply.Invoke("修改成功");
        return true;
    }
    
    public async Task<bool> EnableLoggerChannel(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            throw new InternalProcessException(
                nameof(IslandManagerCommand),
                nameof(EnableLoggerChannel),
                $"找不到频道 {context.EventInfo.IslandId} 的配置记录");
        }

        settings.EnableChannelLogger = true;
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Reply.Invoke("修改成功");
        return true;
    }
    
    public async Task<bool> DisableLoggerChannel(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            throw new InternalProcessException(
                nameof(IslandManagerCommand),
                nameof(DisableLoggerChannel),
                $"找不到频道 {context.EventInfo.IslandId} 的配置记录");
        }

        settings.EnableChannelLogger = false;
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Reply.Invoke("修改成功");
        return true;
    }

    public async Task<bool> RenewWebApiToken(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();
        
        if (settings is null)
        {
            throw new InternalProcessException(
                nameof(IslandManagerCommand),
                nameof(RenewWebApiToken),
                $"找不到频道 {context.EventInfo.IslandId} 的配置记录");
        }

        settings.WebApiToken = TokenHelper.GenerateToken();
        await collection.FindOneAndReplaceAsync(x => x.IslandId == context.EventInfo.IslandId, settings);
        
        await context.Reply.Invoke("修改成功");
        return true;
    }

    public async Task<bool> GetSettings(
        CommandContext context,
        [Inject] OpenApiService openApiService,
        [Inject] IMongoCollection<IslandSettings> collection)
    {
        var settings = await collection
            .Find(x => x.IslandId == context.EventInfo.IslandId)
            .FirstOrDefaultAsync();

        if (settings is null)
        {
            throw new InternalProcessException(
                nameof(IslandManagerCommand),
                nameof(GetSettings),
                $"找不到频道 {context.EventInfo.IslandId} 的配置记录");
        }

        var card = await settings.GetIslandSettingsCard(openApiService);
        await context.ReplyCard.Invoke(card);
        
        return true;
    }

    public async Task<bool> GetRoles(CommandContext context, [Inject] OpenApiService openApiService)
    {
        var roles = await openApiService
            .GetRoleListAsync(new GetRoleListInput { IslandId = context.EventInfo.IslandId }, true);
        if (roles is null)
        {
            await context.Reply.Invoke("找不到群组身份组信息");
            return false;
        }

        var card = roles.GetRolesCard();
        
        await context.ReplyCard.Invoke(card);

        return true;
    }

    public async Task<bool> GetWebApiToken(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection)
    {
        var token = collection.AsQueryable()
            .FirstOrDefault(x => x.IslandId == context.EventInfo.IslandId)?.WebApiToken;

        if (token is null)
        {
            await context.Reply.Invoke("找不到记录");
            return false;
        }
        
        await context.Reply.Invoke($"Web API Token: `{token}`", true);
        return true;
    }

    public async Task<bool> SendText(CommandContext context,
        [Inject] OpenApiService openApiService,
        [CmdOption("channel", "c", "发送目标频道")] DodoChannelId channel,
        [CmdOption("message", "m", "要发送的消息")] string message,
        [CmdOption("user", "u", "发送只有某个用户看到的的消息", false)] DodoMemberId? user,
        [CmdOption("reply", "r", "回复消息 ID", false)] string? replyId)
    {
        var result = await openApiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyText>
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
        
        if (result is null)
        {
            await context.Reply.Invoke("发送失败");
            return false;
        }

        var replyMsg = user is null
            ? $"发送成功，频道：{channel.Ref}, 消息 ID：`{result.MessageId}`"
            : $"私聊 {user.Value.Ref} 的消息发送成功，频道：{channel.Ref}, 消息 ID：`{result.MessageId}`";
        await context.Reply.Invoke(replyMsg);
        
        return true;
    }
    
    public async Task<bool> SendImage(CommandContext context,
        [Inject] OpenApiService openApiService,
        [CmdOption("channel", "c", "发送目标频道")] DodoChannelId channel,
        [CmdOption("image", "i", "要发送的图片链接")] string url,
        [CmdOption("user", "u", "发送只有某个用户看到的的消息", false)] DodoMemberId? user,
        [CmdOption("reply", "r", "回复消息 ID", false)] string? replyId)
    {
        var response = await openApiService
            .SetResourcePictureUploadAsync(new SetResourceUploadInput { FilePath = url }, true);
        
        var result = await openApiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyPicture>
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

        if (result is null)
        {
            await context.Reply.Invoke("发送失败");
            return false;
        }

        var replyMsg = user is null
            ? $"发送成功，频道：{channel.Ref}, 消息 ID：`{result.MessageId}`"
            : $"私聊 {user.Value.Ref} 的消息发送成功，频道：{channel.Ref}, 消息 ID：`{result.MessageId}`";
        await context.Reply.Invoke(replyMsg);

        return true;
    }

    public async Task<bool> SendReaction(CommandContext context,
        [Inject] OpenApiService openApiService,
        [CmdOption("id", "i", "消息 ID")] string msgId,
        [CmdOption("emoji", "e", "反应 Emoji")] DodoEmoji emoji)
    {
        var result = await openApiService.SetChannelMessageReactionAddAsync(new SetChannelMessageReactionAddInput
        {
            MessageId = msgId, Emoji = new MessageModelEmoji { Id = emoji.EmojiId.ToString(), Type = 1 }
        }, true);

        await context.Reply.Invoke(result ? $"在消息 `{msgId}` 添加 `{emoji.Emoji}` 反应成功" : "添加失败");
        
        return true;
    }
    
    public async Task<bool> DeleteMessage(CommandContext context,
        [Inject] OpenApiService openApiService,
        [CmdOption("id", "i", "消息 ID")] string msgId,
        [CmdOption("reason", "r", "删除原因", false)] string? reason)
    {
        var result = await openApiService.SetChannelMessageWithdrawAsync(new SetChannelMessageWithdrawInput
        {
            MessageId = msgId, Reason = reason
        }, true);

        await context.Reply.Invoke(result ? $"删除消息 `{msgId}` 成功，原因：「{reason}」" : "移除失败");
        
        return true;
    }
    
    public async Task<bool> DeleteReaction(CommandContext context,
        [Inject] OpenApiService openApiService,
        [CmdOption("id", "i", "消息 ID")] string msgId,
        [CmdOption("emoji", "e", "反应 Emoji")] DodoEmoji emoji)
    {
        var result = await openApiService.SetChannelMessageReactionRemoveAsync(new SetChannelMessageReactionRemoveInput
        {
            MessageId = msgId, Emoji = new MessageModelEmoji { Id = emoji.EmojiId.ToString(), Type = 1 }
        }, true);
        
        await context.Reply.Invoke(result ? $"从消息 `{msgId}` 移除 `{emoji.Emoji}` 反应成功" : "移除失败");
        
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
}
