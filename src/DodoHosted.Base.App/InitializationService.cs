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

using DoDo.Open.Sdk.Models.Islands;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.App.Entities;
using DodoHosted.Base.App.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace DodoHosted.Base.App;

public class InitializationService : IHostedService
{
    private readonly IMongoDatabase _database;
    private readonly OpenApiService _openApiService;
    private readonly ILogger<InitializationService> _logger;

    public InitializationService(
        IMongoDatabase database,
        OpenApiService openApiService,
        ILogger<InitializationService> logger)
    {
        _database = database;
        _openApiService = openApiService;
        _logger = logger;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var collection = _database.GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);

        var islands = await _openApiService.GetIslandListAsync(new GetIslandListInput());

        if (islands is null)
        {
            _logger.LogCritical("初始化失败，获取群组列表失败");
            Environment.Exit(-1);
        }

        var ids = islands.Select(x => x.IslandId).ToArray();

        var stored = collection.AsQueryable().ToList().Select(x => x.IslandId).ToList();
        var removed = stored
            .SkipWhile(x => ids.Contains(x))
            .ToList();
        var added = ids
            .SkipWhile(x => stored.Contains(x))
            .Select(x => new IslandSettings(x, false, string.Empty))
            .ToList();

        if (removed.Count > 0)
        {
            await collection.DeleteManyAsync(Builders<IslandSettings>.Filter.In(x => x.IslandId, removed), cancellationToken);
        }

        if (added.Count > 0)
        {
            await collection.InsertManyAsync(added, cancellationToken: cancellationToken);
        }
        
        _logger.LogInformation("初始化完成，移除 {RemovedIslandCount} 个群组，新增 {AddedIslandCount} 群组", removed.Count, added.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
