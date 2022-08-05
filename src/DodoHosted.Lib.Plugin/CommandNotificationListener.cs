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

using DoDo.Open.Sdk.Models.Messages;
using DodoHosted.Base;
using DodoHosted.Base.Core.Notifications;
using DodoHosted.Base.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.Plugin;

public class CommandNotificationListener : INotificationHandler<DodoChannelMessageNotification<MessageBodyText>>
{
    private readonly IPluginManager _pluginManager;
    private readonly ILogger<CommandNotificationListener> _logger;

    public CommandNotificationListener(IPluginManager pluginManager, ILogger<CommandNotificationListener> logger)
    {
        _pluginManager = pluginManager;
        _logger = logger;
    }
    
    public async Task Handle(DodoChannelMessageNotification<MessageBodyText> notification, CancellationToken cancellationToken)
    {
        var message = notification.Message.Data.EventBody.MessageBody.Content;

        if (message.StartsWith(HostEnvs.CommandPrefix) is false)
        {
            return;
        }
        
        var eventBody = notification.Message.Data.EventBody;
        var cmdMessage = new CommandMessage
        {
            IslandId = eventBody.IslandId,
            ChannelId = eventBody.ChannelId,
            EventId = notification.Message.Data.EventId,
            MemberId = eventBody.DodoId,
            MessageId = eventBody.MessageId,
            MemberLevel = eventBody.Member.Level,
            MemberJoinTime = DateTimeOffset.Parse(eventBody.Member.JoinTime),
            PersonalNickname = eventBody.Personal.NickName,
            MemberNickname = eventBody.Member.NickName,
            OriginalText = eventBody.MessageBody.Content,
        };
        _logger.LogTrace("接收到指令：{TraceCommandMessageReceived}，发送者：{TraceCommandSender}", message, cmdMessage.PersonalNickname);

        await _pluginManager.RunCommand(cmdMessage);
    }
}
