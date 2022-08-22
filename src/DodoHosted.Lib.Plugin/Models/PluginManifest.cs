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

using System.Reflection;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Lib.Plugin.Models;

/// <summary>
/// 插件属性清单
/// </summary>
public record PluginManifest
{
    /// <summary>
    /// 插件入口 Assembly
    /// </summary>
    public required Assembly PluginEntryAssembly { get; init; }
    
    /// <summary>
    /// 插件 Assembly 加载上下文
    /// </summary>
    public required PluginAssemblyLoadContext Context { get; init; }

    /// <summary>
    /// 插件信息
    /// </summary>
    public required PluginInfo PluginInfo { get; init; }
    
    /// <summary>
    /// 插件生命周期 Scope
    /// </summary>
    public required IServiceScope PluginScope { get; init; }
    
    /// <summary>
    /// 插件生命周期
    /// </summary>
    public required IPluginLifetime? PluginLifetime { get; init; }
    
    /// <summary>
    /// 插件所含 Event Handler
    /// </summary>
    public required EventHandlerManifest[] EventHandlers { get; init; }
    
    /// <summary>
    /// 插件所含指令
    /// </summary>
    public required CommandManifest[] CommandManifests { get; init; }
    
    /// <summary>
    /// 插件所含后台服务
    /// </summary>
    public required HostedServiceManifest[] HostedServices { get; init; }
}
