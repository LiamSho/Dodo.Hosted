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

using DoDo.Open.Sdk.Models;

namespace DodoHosted.Lib.SdkWrapper.Builder;

/// <summary>
/// 构建 <see cref="OpenEventOptions"/>.
/// </summary>
public class DodoOpenEventOptionsBuilder
{
    private readonly OpenEventOptions _options;

    public DodoOpenEventOptionsBuilder()
    {
        _options = new OpenEventOptions { IsAsync = false, IsReconnect = false };
    }

    /// <summary>
    /// 使用异步处理.
    /// </summary>
    /// <returns></returns>
    public DodoOpenEventOptionsBuilder UseAsync()
    {
        _options.IsAsync = true;
        return this;
    }

    /// <summary>
    /// 允许断线重连.
    /// </summary>
    /// <returns></returns>
    public DodoOpenEventOptionsBuilder UseReconnect()
    {
        _options.IsReconnect = true;
        return this;
    }

    /// <summary>
    /// 构建 <see cref="OpenEventOptions"/>.
    /// </summary>
    /// <returns></returns>
    internal OpenEventOptions Build()
    {
        return _options;
    }
}
