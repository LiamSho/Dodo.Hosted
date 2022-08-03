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
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.Interfaces;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Base.Services;

public class ChannelLogger : IChannelLogger
{
    private readonly ILogger<ChannelLogger> _logger;
    private readonly OpenApiService _apiService;

    public ChannelLogger(ILogger<ChannelLogger> logger, OpenApiService apiService)
    {
        _logger = logger;
        _apiService = apiService;
    }

    private static string CurrentTime => DateTimeOffset.UtcNow.AddHours(8).ToString("yyyy-MM-dd hh:mm:ss");

    private static string GetLogLevelFormatted(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        LogLevel.None => "NON",
        _ => throw new ArgumentOutOfRangeException(nameof(logLevel), logLevel, null)
    };

    private void SendChannelMessage(LogLevel logLevel, string message)
    {
        if (HostEnvs.DodoHostedChannelLogEnabled is false)
        {
            return;
        }
        
        _apiService.SetChannelMessageSend(new SetChannelMessageSendInput<MessageBodyText>
        {
            ChannelId = HostEnvs.DodoHostedChannelLogChannelId, MessageBody = new MessageBodyText
            {
                Content = $"[{CurrentTime}] `{GetLogLevelFormatted(logLevel)}` {message}"
            }
        });
    }

    public void LogTrace(string message)
    {
        _logger.LogTrace("Channel logger: {ChannelLoggingMessage}", message);
        SendChannelMessage(LogLevel.Trace, message);
    }
    
    public void LogDebug(string message)
    {
        _logger.LogDebug("Channel logger: {ChannelLoggingMessage}", message);
        SendChannelMessage(LogLevel.Debug, message);
    }
    
    public void LogInformation(string message)
    {
        _logger.LogInformation("Channel logger: {ChannelLoggingMessage}", message);
        SendChannelMessage(LogLevel.Information, message);
    }

    public void LogWarning(string message)
    {
        _logger.LogWarning("Channel logger: {ChannelLoggingMessage}", message);
        SendChannelMessage(LogLevel.Warning, message);
    }

    public void LogError(string message)
    {
        _logger.LogError("Channel logger: {ChannelLoggingMessage}", message);
        SendChannelMessage(LogLevel.Error, message);
    }
    
    public void LogCritical(string message)
    {
        _logger.LogCritical("Channel logger: {ChannelLoggingMessage}", message);
        SendChannelMessage(LogLevel.Critical, message);
    }
}
