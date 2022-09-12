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

using System.Diagnostics;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Members;
using DoDo.Open.Sdk.Models.Messages;
using DodoHosted.Base;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.Card;
using DodoHosted.Base.Enums;
using DodoHosted.Base.Events;
using DodoHosted.Lib.Plugin.Helper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.Plugin;

public partial class PluginManager
{
    /// <inheritdoc />
    public async Task RunCommand(DodoChannelMessageEvent<MessageBodyText> messageEvent)
    {
        var message = messageEvent.Message.Data.EventBody.MessageBody.Content;

        if (message.StartsWith(HostEnvs.CommandPrefix) is false)
        {
            return;
        }

        var sw = Stopwatch.StartNew();
        
        var eventBody = messageEvent.Message.Data.EventBody;
        var userInfo = new PluginBase.UserInfo(
            eventBody.Personal.NickName,
            eventBody.Personal.AvatarUrl,
            (Sex)eventBody.Personal.Sex,
            eventBody.Member.NickName,
            eventBody.Member.Level,
            DateTimeOffset.Parse(eventBody.Member.JoinTime),
            eventBody.DodoId);
        var eventInfo = new PluginBase.EventInfo(
            eventBody.IslandId,
            eventBody.ChannelId,
            eventBody.MessageId,
            messageEvent.Message.Data.EventId,
            messageEvent.Message.Data.Timestamp);
        var text = eventBody.MessageBody.Content;
        
        _logger.LogTrace("接收到指令：{TraceCommandMessageReceived}，发送者：{TraceCommandSender}", message, userInfo.NickName);

        PluginBase.Reply reply = async delegate(string content, bool privateMessage)
        {
            var replySw = Stopwatch.StartNew();
            _logger.LogTrace("回复{TracePrivateMessage}消息 {TraceReplyTargetId}", privateMessage ? "私密" : "非私密", content);
            var output = await _openApiService.SetChannelMessageSendAsync(
                new SetChannelMessageSendInput<MessageBodyText>
                {
                    ChannelId = eventInfo.ChannelId,
                    MessageBody = new MessageBodyText { Content = content },
                    ReferencedMessageId = eventInfo.MessageId,
                    DodoId = privateMessage ? userInfo.DodoId : null
                });
            replySw.Stop();
            _logger.LogTrace("已回复消息，耗时 {TraceReplyTime} MS，消息 ID 为 {TraceReplyMessageId}",
                replySw.ElapsedMilliseconds, output.MessageId);
            
            return output.MessageId;
        };
        
        var parsed = text.GetCommandArgs();
        if (parsed is null)
        {
            await reply.Invoke("指令解析失败");
            _logger.LogWarning("指令解析失败，指令：{WarningCommandParseFailed}", text);
            return;
        }

        _logger.LogTrace("已解析接收到的指令：{TraceCommandParsedName} {TraceCommandParsedPath} {TraceCommandParsedArgs}",
            parsed.CommandName,
            $"[{string.Join(", ", parsed.Path)}]",
            $"[{string.Join(",", parsed.Arguments.Select(x => $"{{{x.Key}:{x.Value}}}"))}]");

        var cmdInfo = AllCommands.FirstOrDefault(x => x.RootNode.Value == parsed.CommandName);

        if (cmdInfo is null)
        {
            _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，发送者 {CommandSender}，频道 {CommandSendChannel}，消息 {CommandMessage}",
                text, CommandExecutionResult.Unknown, $"{userInfo.NickName} ({userInfo.DodoId})", eventInfo.ChannelId, eventInfo.MessageId);
            await _channelLogger.LogWarning(eventInfo.IslandId, $"指令不存在：`{text}`，" +
                                      $"发送者：<@!{userInfo.DodoId}>，" +
                                      $"频道：<#{eventInfo.ChannelId}>，" +
                                      $"消息 ID：`{eventInfo.MessageId}`");
            await reply.Invoke($"指令 `{text}` 不存在，执行 `{HostEnvs.CommandPrefix}help` 查看所有可用指令");
            return;
        }

        var senderRoles = await GetMemberRole(userInfo.DodoId, eventInfo.IslandId);

        var scope = _provider.CreateScope();
        
        var permissionManager = scope.ServiceProvider.GetRequiredService<IPermissionManager>();
        var result = CommandExecutionResult.Failed;

        async Task<string> ReplyCard(CardMessage cardMessage, bool privateMessage)
        {
            var replySw = Stopwatch.StartNew();
            _logger.LogTrace("回复{TracePrivateMessage}卡片消息", privateMessage ? "私密" : "非私密");
            var output = await _openApiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyCard>
            {
                ChannelId = eventInfo.ChannelId,
                MessageBody = cardMessage.Serialize(),
                ReferencedMessageId = eventInfo.MessageId,
                DodoId = privateMessage ? userInfo.DodoId : null
            });
            replySw.Stop();
            _logger.LogTrace("已回复卡片消息，耗时 {TraceReplyTime} MS，消息 ID 为 {TraceReplyMessageId}",
                replySw.ElapsedMilliseconds, output.MessageId);

            return output.MessageId;
        }
        async Task<bool> PermissionCheck(string node) =>
            IsSuperAdmin(senderRoles) ||
            await permissionManager.CheckPermission(node, senderRoles, eventInfo.IslandId, eventInfo.ChannelId);

        var context = new PluginBase.Context(
            new PluginBase.Functions(reply, ReplyCard, PermissionCheck),
            userInfo, eventInfo, _openApiService, scope.ServiceProvider);
        
        try
        {
            result = await cmdInfo.Invoke(parsed, context);
        }
        catch (Exception ex)
        {
            await reply.Invoke($"指令执行出错，Exception：`{ex.GetType().FullName}`，Message：{ex.Message}");
            await _channelLogger.LogError(eventInfo.IslandId,
                $"指令执行出错：`{text}`，" +
                $"发送者：<@!{userInfo.DodoId}>，" +
                $"频道：<#{eventInfo.ChannelId}>，" +
                $"消息 ID：`{eventInfo.MessageId}`，" +
                $"Exception：`{ex.GetType().FullName}`，" +
                $"Message：{ex.Message}");
        }
        
        _logger.LogTrace("指令执行结果：{TraceCommandExecutionResult}", result);
        switch (result)
        {
            case CommandExecutionResult.Success:
            case CommandExecutionResult.Failed:
            case CommandExecutionResult.Unknown:
                break;
            case CommandExecutionResult.Unauthorized:
                await _channelLogger.LogWarning(eventInfo.IslandId, $"无权访问：`{text}`，" +
                                          $"发送者：<@!{userInfo.DodoId}>，" +
                                          $"频道：<#{eventInfo.ChannelId}>，" +
                                          $"消息 ID：`{eventInfo.MessageId}`");
                break;
            default:
                await _channelLogger.LogError(eventInfo.IslandId, $"未知的指令执行结果：`{result}`");
                break;
        }
        
        scope.Dispose();
        
        sw.Stop();
        
        _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，" +
                               "耗时：{CommandExecutionTime} MS，发送者：{CommandSender}，" +
                               "频道：{CommandSendChannel}，消息：{CommandMessage}",
            text, result, sw.ElapsedMilliseconds, $"{userInfo.NickName} ({userInfo.DodoId})",
            eventInfo.ChannelId, eventInfo.MessageId);
    }

    /// <summary>
    /// 获取用户身份组列表
    /// </summary>
    /// <param name="dodoId">用户 ID</param>
    /// <param name="islandId">群组 ID</param>
    /// <returns></returns>
    private async Task<List<GetMemberRoleListOutput>> GetMemberRole(string dodoId, string islandId)
    {
        var sw = Stopwatch.StartNew();
        var senderRoles = await _openApiService.GetMemberRoleListAsync(new GetMemberRoleListInput
        {
            DodoId = dodoId, IslandId = islandId
        });
        sw.Stop();
        _logger.LogTrace("请求 DodoApi 获取身份组耗时：{TraceFetchMemberRoles} MS", sw.ElapsedMilliseconds);
        
        return senderRoles;
    }

    /// <summary>
    /// 是否有超级管理员权限
    /// </summary>
    /// <param name="roles">权限组列表</param>
    /// <returns></returns>
    private static bool IsSuperAdmin(IEnumerable<GetMemberRoleListOutput> roles)
    {
        return roles
            .Select(x => Convert.ToInt32(x.Permission, 16))
            .Any(x => (x >> 3) % 2 == 1);
    }
}
