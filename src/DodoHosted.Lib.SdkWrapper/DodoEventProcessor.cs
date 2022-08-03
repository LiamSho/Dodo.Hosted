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

using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.Core.Notifications;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.SdkWrapper;

public class DodoEventProcessor : EventProcessService
{
    private readonly ILogger<DodoEventProcessor> _logger;
    private readonly IMediator _mediator;

    public DodoEventProcessor(ILogger<DodoEventProcessor> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }
    
    public override void Connected(string message)
    {
        _logger.LogInformation("DodoEventProcessor {DodoEventType}: {DodoEventMessage}", nameof(Connected), message);
    }

    public override void Disconnected(string message)
    {
        _logger.LogInformation("DodoEventProcessor {DodoEventType}: {DodoEventMessage}", nameof(Disconnected), message);
    }

    public override void Reconnected(string message)
    {
        _logger.LogInformation("DodoEventProcessor {DodoEventType}: {DodoEventMessage}", nameof(Reconnected), message);
    }

    public override void Exception(string message)
    {
        _logger.LogError("DodoEventProcessor {DodoEventType}: {DodoEventMessage}", nameof(Exception), message);
    }

    public override void ChannelMessageEvent<T>(EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelMessage<T>>> input)
    {
        _mediator.Publish(new DodoChannelMessageNotification<T>(input)).GetAwaiter().GetResult();
    }

    public override void MemberJoinEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMemberJoin>> input)
    {
        _mediator.Publish(new DodoMemberJoinNotification(input)).GetAwaiter().GetResult();
    }

    public override void MemberLeaveEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMemberLeave>> input)
    {
        _mediator.Publish(new DodoMemberLeaveNotification(input)).GetAwaiter().GetResult();
    }

    public override void MessageReactionEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMessageReaction>> input)
    {
        _mediator.Publish(new DodoMessageReactionNotification(input)).GetAwaiter().GetResult();
    }

    public override void PersonalMessageEvent<T>(EventSubjectOutput<EventSubjectDataBusiness<EventBodyPersonalMessage<T>>> input)
    {
        _mediator.Publish(new DodoPersonalMessageNotification<T>(input)).GetAwaiter().GetResult();
    }
}
