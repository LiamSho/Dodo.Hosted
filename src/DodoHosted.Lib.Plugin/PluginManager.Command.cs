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
using DodoHosted.Base.App;
using DodoHosted.Base.App.Models;
using DodoHosted.Base.Events;
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

        var sw = Stopwatch.StartNew();
        
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
            var replySw = Stopwatch.StartNew();
            _logger.LogTrace("回复消息 {TraceReplyTargetId}", s);
            var output = await _openApiService.SetChannelMessageSendAsync(
                new SetChannelMessageSendInput<MessageBodyText>
                {
                    ChannelId = cmdMessage.ChannelId,
                    MessageBody = new MessageBodyText { Content = s, },
                    ReferencedMessageId = cmdMessage.MessageId
                });
            replySw.Stop();
            _logger.LogTrace("已回复消息，耗时 {TraceReplyTime} MS，消息 ID 为 {TraceReplyMessageId}", replySw.ElapsedMilliseconds, output.MessageId);
            
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
        
        sw.Stop();
        
        _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，耗时：{CommandExecutionTime} MS，发送者：{CommandSender}，频道：{CommandSendChannel}，消息：{CommandMessage}",
            cmdMessage.OriginalText, result, sw.ElapsedMilliseconds, $"{cmdMessage.PersonalNickname} ({cmdMessage.MemberId})", cmdMessage.ChannelId, cmdMessage.MessageId);
    }
    
    /// <summary>
    /// 解析获取指令参数
    /// </summary>
    /// <param name="commandMessage"></param>
    /// <returns></returns>
    private static IEnumerable<string> GetCommandArgs(string commandMessage)
    {
        if (string.IsNullOrEmpty(commandMessage) || commandMessage.Length < 2)
        {
            return Array.Empty<string>();
        }
        
        var args = new List<string>();
        var command = commandMessage[1..].TrimEnd().AsSpan();
        var startPointer = 0;
    
        var inQuote = false;
            
        // /cmd "some thing \"in\" quote" value
        // cmd | some thing "in" quote | value
            
        for (var movePointer = 0; movePointer < command.Length; movePointer++)
        {
            if (command[movePointer] == '"')
            {
                if (movePointer == 0)
                {
                    return new[] { commandMessage[1..] };
                }
                    
                if (command[movePointer - 1] == '\\')
                {
                    continue;
                }
                    
                inQuote = !inQuote;
            }
                
            if (command[movePointer] != ' ')
            {
                continue;
            }
    
            if (inQuote)
            {
                continue;
            }
    
            if (command[movePointer - 1] == '"')
            {
                args.Add(command.Slice(startPointer + 1, movePointer - startPointer - 2)
                    .ToString()
                    .Replace("\\", string.Empty));
            }
            else
            {
                args.Add(command.Slice(startPointer, movePointer - startPointer).ToString());
            }
            startPointer = movePointer + 1;
        }
            
        args.Add(command[startPointer..].ToString());

        return args;
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
    /// 是否用哟超级管理员权限组
    /// </summary>
    /// <param name="roles">权限组列表</param>
    /// <returns></returns>
    private static bool IsSuperAdmin(IEnumerable<MemberRole> roles)
    {
        return roles.Any(x => (x.Permission >> 3) % 2 == 1);
    }
}
