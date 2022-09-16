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

using DodoHosted.Base.App.Command;
using DodoHosted.Open.Plugin;

namespace DodoHosted.Lib.Plugin.Models.Manifest;

/// <summary>
/// 指令清单
/// </summary>
public record CommandManifest
{
    /// <summary>
    /// 指令执行器
    /// </summary>
    public required ICommandExecutor CommandExecutor { get; set; }
    
    /// <summary>
    /// 指令方法
    /// </summary>
    public required CommandNode RootNode { get; set; }
}
