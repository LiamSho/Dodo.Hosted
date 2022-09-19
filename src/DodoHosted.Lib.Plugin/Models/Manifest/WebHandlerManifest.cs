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
/// Web 请求 Handler 清单
/// </summary>
public record WebHandlerManifest
{
    /// <summary>
    /// Web Handler 类型
    /// </summary>
    public required Type WebHandlerType { get; init; }
    
    /// <summary>
    /// Web Handler 名称
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// <see cref="WebHandlerType"/> 构造函数
    /// </summary>
    public required ConstructorInfo WebHandlerConstructor { get; init; }
    
    /// <summary>
    /// <see cref="IPluginWebHandler.HandleAsync"/> 方法
    /// </summary>
    public required MethodInfo HandlerMethod { get; init; }
    
    /// <summary>
    /// <see cref="PluginInfo.Identifier"/>
    /// </summary>
    public required string PluginIdentifier { get; init; }
}
