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

using DnsClient.Internal;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Messages;
using DodoHosted.Base;
using DodoHosted.Base.Events;
using DodoHosted.Base.Models;
using DodoHosted.Open.Plugin;
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
        
        var eventBody = messageEvent.Message.Data.EventBody;
        var cmdMessage = new CommandMessage
        {
            IslandId = eventBody.IslandId,
            ChannelId = eventBody.ChannelId,
            EventId = messageEvent.Message.Data.EventId,
            MemberId = eventBody.DodoId,
            MessageId = eventBody.MessageId,
            MemberLevel = eventBody.Member.Level,
            MemberJoinTime = DateTimeOffset.Parse(eventBody.Member.JoinTime),
            PersonalNickname = eventBody.Personal.NickName,
            MemberNickname = eventBody.Member.NickName,
            OriginalText = eventBody.MessageBody.Content,
        };
        _logger.LogTrace("接收到指令：{TraceCommandMessageReceived}，发送者：{TraceCommandSender}", message, cmdMessage.PersonalNickname);

        var args = GetCommandArgs(cmdMessage.OriginalText).ToArray();
        _logger.LogTrace("已解析接收到的指令：{TraceReceivedCommandParsed}", $"[{string.Join(", ", args)}]");
        if (args.Length == 0)
        {
            return;
        }

        var command = args[0];
        var cmdInfo = AllCommands.FirstOrDefault(x => x.Name == command);
        var reply = async Task<string>(string s) =>
        {
            _logger.LogTrace("回复消息 {TraceReplyTargetId}", s);
            var output = await _openApiService.SetChannelMessageSendAsync(
                new SetChannelMessageSendInput<MessageBodyText>
                {
                    ChannelId = cmdMessage.ChannelId,
                    MessageBody = new MessageBodyText { Content = s, },
                    ReferencedMessageId = cmdMessage.MessageId
                });
            _logger.LogTrace("已回复消息, 消息 ID 为 {TraceReplyMessageId}", output.MessageId);
            
            return output.MessageId;
        };
        
        if (cmdInfo is null)
        {
            _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，发送者 {CommandSender}，频道 {CommandSendChannel}，消息 {CommandMessage}",
                cmdMessage.OriginalText, CommandExecutionResult.Unknown, $"{cmdMessage.PersonalNickname} ({cmdMessage.MemberId})", cmdMessage.ChannelId, cmdMessage.MessageId);
            _channelLogger.LogWarning($"指令不存在：`{cmdMessage.OriginalText}`，" +
                                      $"发送者：<@!{cmdMessage.MemberId}>，" +
                                      $"频道：<#{cmdMessage.ChannelId}>，" +
                                      $"消息 ID：`{cmdMessage.MessageId}`");
            await reply.Invoke($"指令 `{cmdMessage.OriginalText}` 不存在，执行 `{HostEnvs.CommandPrefix}help` 查看所有可用指令");
            return;
        }

        var senderRoles = await GetMemberRole(cmdMessage.MemberId, cmdMessage.IslandId);

        cmdMessage.Roles = senderRoles
            .Select(x =>
                new MemberRole
                {
                    Id = x.RoleId,
                    Name = x.RoleName,
                    Color = x.RoleColor,
                    Position = x.Position,
                    Permission = Convert.ToInt32(x.Permission, 16)
                })
            .ToList();
        
        var result = await cmdInfo.CommandExecutor.Execute(args, cmdMessage, _provider, reply,IsSuperAdmin(cmdMessage.Roles));
        _logger.LogTrace("指令执行结果：{TraceCommandExecutionResult}", result);

        switch (result)
        {
            case CommandExecutionResult.Success:
            case CommandExecutionResult.Failed:
                break;
            case CommandExecutionResult.Unknown:
                await reply.Invoke($"指令 `{cmdMessage.OriginalText}` 不存在或存在格式错误\n\n" +
                                   $"指令 `{HostEnvs.CommandPrefix}{args[0]}` 的帮助描述：\n\n" +
                                   cmdInfo.HelpText);
                break;
            case CommandExecutionResult.Unauthorized:
                _channelLogger.LogWarning($"无权访问：`{cmdMessage.OriginalText}`，" +
                                          $"发送者：<@!{cmdMessage.MemberId}>，" +
                                          $"频道：<#{cmdMessage.ChannelId}>，" +
                                          $"消息 ID：`{cmdMessage.MessageId}`");
                break;
            default:
                _channelLogger.LogError($"未知的指令执行结果：`{result}`");
                break;
        }
        
        _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，发送者 {CommandSender}，频道 {CommandSendChannel}，消息 {CommandMessage}",
            cmdMessage.OriginalText, result, $"{cmdMessage.PersonalNickname} ({cmdMessage.MemberId})", cmdMessage.ChannelId, cmdMessage.MessageId);
    }

}
