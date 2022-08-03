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

using System.Text.Json.Serialization;

namespace DodoHosted.Lib.Plugin.Models;

/// <summary>
/// 插件信息
/// </summary>
public record PluginInfo
{
    /// <summary>
    /// 标识符
    /// </summary>
    [JsonPropertyName("identifier")]
    public required string Identifier { get; init; }
    
    /// <summary>
    /// 名称
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    /// <summary>
    /// 版本
    /// </summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }
    
    /// <summary>
    /// 简介
    /// </summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }
    
    /// <summary>
    /// 作者
    /// </summary>
    [JsonPropertyName("author")]
    public required string Author { get; init; }
    
    /// <summary>
    /// 入口 Assembly 名称，不带有文件扩展名
    /// </summary>
    [JsonPropertyName("entry_assembly")]
    public required string EntryAssembly { get; init; }
}
