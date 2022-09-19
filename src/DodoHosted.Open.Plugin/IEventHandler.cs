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
using DodoHosted.Base.Context;

namespace DodoHosted.Open.Plugin;

/// <summary>
/// Event Handler 接口
/// </summary>
/// <typeparam name="T">Event Handler 所处理的 Event 类型</typeparam>
/// <remarks>
/// 实现类不能包涵泛型参数
/// </remarks>
public interface IEventHandler<in T> where T : IDodoHostedEvent
{
    /// <summary>
    /// 处理 Event
    /// </summary>
    /// <param name="eventContext">Event Context</param>
    /// <returns></returns>
    Task Handle(T eventContext);
}
