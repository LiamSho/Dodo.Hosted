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

using DnsClient.Internal;

namespace DodoHosted.Base.App;

public record HostConfiguration
{
    /// <summary>
    /// <see cref="HostEnvs.PluginCacheDirectory"/>
    /// </summary>
    public string? PluginCacheDirectory { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.PluginDirectory"/>
    /// </summary>
    public string? PluginDirectory { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.MongoDbConnectionString"/>
    /// </summary>
    public string? MongoDbConnectionString { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.MongoDbDatabaseName"/>
    /// </summary>
    public string? MongoDbDatabaseName { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.DodoSdkApiEndpoint"/>
    /// </summary>
    public string? DodoSdkApiEndpoint { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.DodoSdkBotClientId"/>
    /// </summary>
    public string? DodoSdkBotClientId { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.DodoSdkBotToken"/>
    /// </summary>
    public string? DodoSdkBotToken { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.DodoHostedAdminIsland"/>
    /// </summary>
    public string? DodoHostedAdminIsland { get; set; }

    /// <summary>
    /// <see cref="HostEnvs.CommandPrefix"/>
    /// </summary>
    public string? CommandPrefix { get; set; }
    
    /// <summary>
    /// <see cref="HostEnvs.DodoHostedOpenApiLogLevel"/>
    /// </summary>
    public LogLevel? DodoHostedOpenApiLogLevel { get; set; }
}
