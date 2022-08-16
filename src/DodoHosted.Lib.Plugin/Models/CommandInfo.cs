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

namespace DodoHosted.Lib.Plugin.Models;

/// <summary>
/// 指令基本信息
/// </summary>
public record CommandInfo
{
    /// <summary>
    /// 名称
    /// </summary>
    public required string Name { get; set; }
    
    /// <summary>
    /// 简介
    /// </summary>
    public required string Description { get; set; }
    
    /// <summary>
    /// 帮助文本
    /// </summary>
    public required string HelpText { get; set; }
    
    /// <summary>
    /// 权限节点文本
    /// </summary>
    public required string PermissionNodesText { get; set; }
}
