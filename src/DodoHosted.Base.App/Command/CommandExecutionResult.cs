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

namespace DodoHosted.Base.App.Command;

public enum CommandExecutionResult
{
    /// <summary>
    /// 执行成功
    /// </summary>
    Success,
    
    /// <summary>
    /// 执行失败，发送此项会向用户发送指令执行失败的消息
    /// </summary>
    Failed,
    
    /// <summary>
    /// 未知的指令，返回此项会向用户发送当前指令的帮助文档
    /// </summary>
    Unknown,
    
    /// <summary>
    /// 权限不足，发送此项将会在日志频道记录消息
    /// </summary>
    Unauthorized,
}
