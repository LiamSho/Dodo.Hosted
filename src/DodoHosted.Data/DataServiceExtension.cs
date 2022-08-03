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

using DodoHosted.Base;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using StackExchange.Redis;

namespace DodoHosted.Data;

public static class DataServiceExtension
{
    public static IServiceCollection AddDataServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<IMongoDatabase>(_ =>
        {
            var mongoClient = new MongoClient(HostEnvs.MongoDbConnectionString);
            return mongoClient.GetDatabase(HostEnvs.MongoDbDatabaseName);
        });

        serviceCollection.AddSingleton(_ =>
        {
            var multiplexer = ConnectionMultiplexer.Connect(HostEnvs.RedisConnectionConfiguration);
            return multiplexer.GetDatabase(HostEnvs.RedisDatabaseId);
        });
        
        return serviceCollection;
    }
}
