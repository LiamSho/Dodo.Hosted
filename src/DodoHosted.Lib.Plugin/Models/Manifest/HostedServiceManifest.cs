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

namespace DodoHosted.Lib.Plugin.Models.Manifest;

/// <summary>
/// 后台服务清单
/// </summary>
public record HostedServiceManifest
{
    /// <summary>
    /// 后台服务类
    /// </summary>
    public required IPluginHostedService Job { get; init; }
    
    /// <summary>
    /// DI Scope
    /// </summary>
    public required IServiceScope Scope { get; init; }
    
    /// <summary>
    /// 后台服务名称
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 后台服务 Task
    /// </summary>
    public Task? JobTask { get; set; }
    
    /// <summary>
    /// 取消令牌源
    /// </summary>
    public CancellationTokenSource CancellationTokenSource { get; } = new();
    
    /// <summary>
    /// <see cref="PluginInfo.Identifier"/>
    /// </summary>
    public required string PluginIdentifier { get; init; }
}
