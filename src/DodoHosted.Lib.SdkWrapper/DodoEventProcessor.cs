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

using System.Text.Json;
using DoDo.Open.Sdk.Models.Events;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base;
using DodoHosted.Base.Events;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.SdkWrapper;

public class DodoEventProcessor : EventProcessService
{
    private readonly ILogger<DodoEventProcessor> _logger;

    public delegate void ProcessEventDelegate(IDodoHostedEvent @event, string typeString);
    public static event ProcessEventDelegate? DodoEvent; 
    
    public DodoEventProcessor(ILogger<DodoEventProcessor> logger)
    {
        _logger = logger;
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
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoChannelMessageEvent<T>(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void MemberJoinEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMemberJoin>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoMemberJoinEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void MemberLeaveEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMemberLeave>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoMemberLeaveEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void MessageReactionEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMessageReaction>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoMessageReactionEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void PersonalMessageEvent<T>(EventSubjectOutput<EventSubjectDataBusiness<EventBodyPersonalMessage<T>>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoPersonalMessageEvent<T>(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void CardMessageListSubmitEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyCardMessageListSubmit>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoCardMessageListSubmitEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void CardMessageButtonClickEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyCardMessageButtonClick>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoCardMessageButtonClickEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void CardMessageFormSubmitEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyCardMessageFormSubmit>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoCardMessageFormSubmitEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void ChannelArticleEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelArticle>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoChannelArticleEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void ChannelArticleCommentEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelArticleComment>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoChannelArticleCommentEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void ChannelVoiceMemberJoinEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelVoiceMemberJoin>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoChannelVoiceMemberJoinEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }

    public override void ChannelVoiceMemberLeaveEvent(EventSubjectOutput<EventSubjectDataBusiness<EventBodyChannelVoiceMemberLeave>> input)
    {
        _logger.LogTrace("DodoEventReceived: {TraceDodoEvent}", JsonSerializer.Serialize(input));
        var e = new DodoChannelVoiceMemberLeaveEvent(input);
        DodoEvent?.Invoke(e, e.GetType().FullName ?? string.Empty);
    }
}
