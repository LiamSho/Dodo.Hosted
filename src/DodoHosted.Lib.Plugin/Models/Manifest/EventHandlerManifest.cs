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
/// Event Handler 清单
/// </summary>
public record EventHandlerManifest
{
    /// <summary>
    /// 继承 <see cref="IDodoHostedPluginEventHandler{T}"/> 的类型
    /// </summary>
    public required Type EventHandlerType { get; init; }
    
    /// <summary>
    /// <see cref="IDodoHostedPluginEventHandler{T}"/> 的泛型类型
    /// </summary>
    public required Type EventType { get; init; }
    
    /// <summary>
    /// <see cref="EventType"/> 的字符串描述
    /// </summary>
    public required string EventTypeString { get; init; }
    
    /// <summary>
    /// <see cref="EventHandlerType"/> 实例
    /// </summary>
    public required object EventHandler { get; init; }
    
    /// <summary>
    /// <see cref="IDodoHostedPluginEventHandler{T}.Handle"/> 方法
    /// </summary>
    public required MethodInfo HandlerMethod { get; init; }
}
