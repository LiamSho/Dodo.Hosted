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

namespace DodoHosted.Lib.Plugin.Interfaces;

public interface IEventManager
{
    /// <summary>
    /// 运行事件处理器
    /// </summary>
    /// <param name="event">事件消息</param>
    /// <param name="typeString">类型字符串</param>
    /// <param name="pluginIdentifier">指定插件 ID</param>
    /// <returns></returns>
    Task<int> RunEvent(IDodoHostedEvent @event, string typeString, string? pluginIdentifier = null);

    /// <summary>
    /// 初始化
    /// </summary>
    void Initialize();
}
