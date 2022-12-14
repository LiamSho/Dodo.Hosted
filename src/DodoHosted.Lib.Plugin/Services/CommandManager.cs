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
using DodoHosted.Base.App.Context;
using DodoHosted.Base.Card;
using DodoHosted.Base.Events;
using DodoHosted.Lib.Plugin.Extensions;

namespace DodoHosted.Lib.Plugin.Services;

public class CommandManager : ICommandManager
{
    private readonly ILogger<CommandManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly IChannelLogger _channelLogger;
    private readonly OpenApiService _openApiService;
    private readonly IPluginManager _pluginManager;

    public CommandManager(
        ILogger<CommandManager> logger,
        IServiceProvider provider,
        IChannelLogger channelLogger,
        OpenApiService openApiService,
        IPluginManager pluginManager)
    {
        _logger = logger;
        _provider = provider;
        _channelLogger = channelLogger;
        _openApiService = openApiService;
        _pluginManager = pluginManager;
    }
    
    /// <inheritdoc />
    public async Task RunCommand(DodoChannelMessageEvent<MessageBodyText> messageEvent)
    {
        var message = messageEvent.Message.Data.EventBody.MessageBody.Content;

        if (message.StartsWith(HostEnvs.CommandPrefix) is false)
        {
            return;
        }

        var sw = Stopwatch.StartNew();

        var eventInfo = messageEvent.GetEventInfo();
        var userInfo = messageEvent.GetUserInfo();

        _logger.LogTrace("接收到指令：{TraceCommandMessageReceived}，发送者：{TraceCommandSender}", message, userInfo.NickName);

        // Reply 委托
        ContextBase.Reply reply = async delegate(string content, bool privateMessage)
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
        
        // 解析指令
        var parsed = message.GetCommandArgs();
        if (parsed is null)
        {
            await reply.Invoke("指令解析失败");
            _logger.LogWarning("指令解析失败，指令：{WarningCommandParseFailed}", message);
            return;
        }

        _logger.LogTrace("已解析接收到的指令：{TraceCommandParsedName} {TraceCommandParsedPath} {TraceCommandParsedArgs}",
            parsed.CommandName,
            $"[{string.Join(", ", parsed.Path)}]",
            $"[{string.Join(",", parsed.Arguments.Select(x => $"{{{x.Key}:{x.Value}}}"))}]");

        // 检查是否为 Admin Island Only
        var adminIslandOnly = _pluginManager.GetCommandNode(parsed.CommandName)?.AdminIslandOnly ?? false;
        if (adminIslandOnly && eventInfo.IslandId != HostEnvs.DodoHostedAdminIsland)
        {
            await _channelLogger.LogWarning(HostEnvs.DodoHostedAdminIsland,
                $"群组 {eventInfo.IslandId} 的用户 {userInfo.NickName} ({userInfo.DodoId}) 尝试" +
                $"执行管理群组限定指令 `{parsed.CommandName}`，原始指令字符串为 `{messageEvent.Message.Data.EventBody.MessageBody.Content}`");
            await reply.Invoke("该指令仅限管理群组使用");
            return;
        }
        
        // 检查是否是帮助请求
        var hasHelpRequest = parsed.Arguments.TryGetValueByMultipleKey(new[] { "-help", "?" }, out var value);

        var originalPath = parsed.Path;
        if (hasHelpRequest && value == "true")
        {
            parsed = new CommandParsed
            {
                CommandName = "help",
                Path = Array.Empty<string>(),
                Arguments = new Dictionary<string, string>
                {
                    { "-name", parsed.CommandName }
                }
            };
        }
        if (originalPath.Length != 0)
        {
            parsed.Arguments.Add("-path", string.Join("," , originalPath));
        }

        var cmd = _pluginManager.GetCommandExecutorModule(parsed.CommandName);
        if (cmd is null)
        {
            _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，发送者 {CommandSender}，频道 {CommandSendChannel}，消息 {CommandMessage}",
                message, CommandExecutionResult.Unknown, $"{userInfo.NickName} ({userInfo.DodoId})", eventInfo.ChannelId, eventInfo.MessageId);
            await _channelLogger.LogWarning(eventInfo.IslandId, $"指令不存在：`{message}`，" +
                                                                $"发送者：<@!{userInfo.DodoId}>，" +
                                                                $"频道：<#{eventInfo.ChannelId}>，" +
                                                                $"消息 ID：`{eventInfo.MessageId}`");
            await reply.Invoke($"指令 `{message}` 不存在，执行 `{HostEnvs.CommandPrefix}help` 查看所有可用指令");
            return;
        }

        // 发送者角色
        var senderRoles = await GetMemberRole(userInfo.DodoId, eventInfo.IslandId);

        // DI 容器 Scope
        var scope = _provider.CreateScope();
        
        var permissionManager = scope.ServiceProvider.GetRequiredService<IPermissionManager>();
        var result = CommandExecutionResult.Failed;

        // ReplyCard 委托
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
        
        // PermissionCheck 委托
        async Task<bool> PermissionCheck(string node) =>
            IsSuperAdmin(senderRoles) ||
            await permissionManager.CheckPermission(node, senderRoles, eventInfo.IslandId, eventInfo.ChannelId);

        // 指令执行上下文
        var context = new CommandContext(reply, ReplyCard, PermissionCheck, parsed, userInfo, eventInfo);

        try
        {
            result = await cmd.Invoke(context, scope.ServiceProvider);
        }
        catch (ParameterResolverException ex)
        {
            await reply.Invoke($"指令参数解析出错：{ex.Message}");
        }
        catch (Exception ex)
        {
            // 出现错误
            await reply.Invoke($"指令执行出错，Exception：`{ex.GetType().FullName}`，Message：{ex.Message}");
            await _channelLogger.LogError(eventInfo.IslandId,
                $"指令执行出错：`{message}`，" +
                $"发送者：<@!{userInfo.DodoId}>，" +
                $"频道：<#{eventInfo.ChannelId}>，" +
                $"消息 ID：`{eventInfo.MessageId}`，" +
                $"Exception：`{ex.GetType().FullName}`，" +
                $"Message：{ex.Message}");
        }
        
        _logger.LogTrace("指令执行结果：{TraceCommandExecutionResult}", result);
        switch (result)
        {
            // 成功
            case CommandExecutionResult.Success:
            // 失败
            case CommandExecutionResult.Failed:
                break;
            // 未知的指令
            case CommandExecutionResult.Unknown:
                await _channelLogger.LogWarning(eventInfo.IslandId, 
                    $"未知的指令：`{message}`，" +
                    $"发送者：<@!{userInfo.DodoId}>，" +
                    $"频道：<#{eventInfo.ChannelId}>，" +
                    $"消息 ID：`{eventInfo.MessageId}`");
                break;
            // 无权访问
            case CommandExecutionResult.Unauthorized:
                await _channelLogger.LogWarning(eventInfo.IslandId, 
                    $"无权访问：`{message}`，" +
                    $"发送者：<@!{userInfo.DodoId}>，" +
                    $"频道：<#{eventInfo.ChannelId}>，" +
                    $"消息 ID：`{eventInfo.MessageId}`");
                break;
            // 未知执行结果
            default:
                await _channelLogger.LogError(eventInfo.IslandId, $"未知的指令执行结果：`{result}`");
                break;
        }
        
        // 释放 Scope
        scope.Dispose();
        
        sw.Stop();
        
        _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，" +
                               "耗时：{CommandExecutionTime} MS，发送者：{CommandSender}，" +
                               "频道：{CommandSendChannel}，消息：{CommandMessage}",
            message, result, sw.ElapsedMilliseconds, $"{userInfo.NickName} ({userInfo.DodoId})",
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
