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
using DodoHosted.Base.App.Entities;
using DodoHosted.Base.App.Interfaces;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DodoHosted.Base.App.Services;

public class ChannelLogger : IChannelLogger
{
    private readonly ILogger<ChannelLogger> _logger;
    private readonly OpenApiService _apiService;
    
    private readonly IMongoCollection<IslandSettings> _collection;

    public ChannelLogger(ILogger<ChannelLogger> logger, OpenApiService apiService, IMongoDatabase database)
    {
        _logger = logger;
        _apiService = apiService;

        _collection = database.GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);
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

    private async Task SendChannelMessage(LogLevel logLevel, string island, string message)
    {
        var info = _collection
            .AsQueryable()
            .FirstOrDefault(x => x.IslandId == island);

        if (info is null || info.EnableChannelLogger is false)
        {
            return;
        }

        await _apiService.SetChannelMessageSendAsync(new SetChannelMessageSendInput<MessageBodyText>
        {
            ChannelId = info.LoggerChannelId, MessageBody = new MessageBodyText
            {
                Content = $"[{CurrentTime}] `{GetLogLevelFormatted(logLevel)}` {message}"
            }
        });
    }

    public async Task LogTrace(string islandId, string message)
    {
        _logger.LogTrace("Channel logger: {ChannelLoggingMessage}", message);
        await SendChannelMessage(LogLevel.Trace, islandId, message);
    }
    
    public async Task LogDebug(string islandId, string message)
    {
        _logger.LogDebug("Channel logger: {ChannelLoggingMessage}", message);
        await SendChannelMessage(LogLevel.Debug, islandId, message);
    }
    
    public async Task LogInformation(string islandId, string message)
    {
        _logger.LogInformation("Channel logger: {ChannelLoggingMessage}", message);
        await SendChannelMessage(LogLevel.Information, islandId, message);
    }

    public async Task LogWarning(string islandId, string message)
    {
        _logger.LogWarning("Channel logger: {ChannelLoggingMessage}", message);
        await SendChannelMessage(LogLevel.Warning, islandId, message);
    }

    public async Task LogError(string islandId, string message)
    {
        _logger.LogError("Channel logger: {ChannelLoggingMessage}", message);
        await SendChannelMessage(LogLevel.Error, islandId, message);
    }
    
    public async Task LogCritical(string islandId, string message)
    {
        _logger.LogCritical("Channel logger: {ChannelLoggingMessage}", message);
        await SendChannelMessage(LogLevel.Critical, islandId, message);
    }
}
