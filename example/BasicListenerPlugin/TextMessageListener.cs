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
using DodoHosted.Base.Events;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BasicListenerPlugin;

public class TextMessageListener : IDodoHostedPluginEventHandler<DodoChannelMessageEvent<MessageBodyText>>
{
    public static bool IsEnabled { get; set; } = true;
    
    public Task Handle(DodoChannelMessageEvent<MessageBodyText> @event, IServiceProvider provider)
    {
        if (IsEnabled is false)
        {
            return Task.CompletedTask;
        }
        
        var logger = provider.GetRequiredService<ILogger<TextMessageListener>>();
        
        logger.LogInformation("Received message: [{Channel}] {Sender}: {Message}",
            @event.Message.Data.EventBody.ChannelId,
            @event.Message.Data.EventBody.Member.NickName,
            @event.Message.Data.EventBody.MessageBody.Content);
        
        return Task.CompletedTask;
    }
}
