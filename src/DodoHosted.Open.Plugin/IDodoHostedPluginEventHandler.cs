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

using DodoHosted.Base;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Open.Plugin;

/// <summary>
/// Event Handler 接口
/// </summary>
/// <typeparam name="T">Event Handler 所处理的 Event 类型</typeparam>
/// <remarks>
/// 实现类不能包涵泛型参数
/// </remarks>
public interface IDodoHostedPluginEventHandler<in T> where T : IDodoHostedEvent
{
    /// <summary>
    /// 处理 Event
    /// </summary>
    /// <param name="event">Event 消息体</param>
    /// <param name="provider">用于访问 DI 容器的 ServiceProvider，对于每次请求，都会使用一个新的 Scope</param>
    /// <param name="logger">日志记录器</param>
    /// <returns></returns>
    Task Handle(T @event, IServiceProvider provider, ILogger logger);
}
