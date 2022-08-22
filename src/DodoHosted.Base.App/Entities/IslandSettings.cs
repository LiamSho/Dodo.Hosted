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

using MongoDB.Bson.Serialization.Attributes;

namespace DodoHosted.Base.App.Entities;

/// <summary>
/// 群组配置
/// </summary>
/// <param name="IslandId"></param>
/// <param name="EnableChannelLogger"></param>
/// <param name="LoggerChannelId"></param>
/// <param name="WebApiToken"></param>
public record IslandSettings(string IslandId, bool EnableChannelLogger, string LoggerChannelId, string WebApiToken)
{
    [BsonId]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    /// 群组 ID
    /// </summary>
    public string IslandId { get; set; } = IslandId;

    /// <summary>
    /// 是否开启 Channel Logger
    /// </summary>
    public bool EnableChannelLogger { get; set; } = EnableChannelLogger;

    /// <summary>
    /// Channel Logger 频道 ID
    /// </summary>
    public string LoggerChannelId { get; set; } = LoggerChannelId;
    
    /// <summary>
    /// Web API Token
    /// </summary>
    public string WebApiToken { get; set; } = WebApiToken;
    
    /// <summary>
    /// 是否允许该群组使用 Web API
    /// </summary>
    public bool AllowUseWebApi { get; set; } = true;
}
