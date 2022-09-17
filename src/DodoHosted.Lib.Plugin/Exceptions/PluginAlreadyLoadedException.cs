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

namespace DodoHosted.Lib.Plugin.Exceptions;

/// <summary>
/// 插件包已载入
/// </summary>
public class PluginAlreadyLoadedException : Exception
{
    public PluginAlreadyLoadedException(PluginInfo exist, PluginInfo readyToLoad)
        : base($"已存在相同标识符的插件，当前已载入：{exist}，待载入：{readyToLoad}") { }
}
