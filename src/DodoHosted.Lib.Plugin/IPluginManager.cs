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

using DodoHosted.Lib.Plugin.Models;

namespace DodoHosted.Lib.Plugin;

/// <summary>
/// 插件管理器
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// 获取已载入插件的信息
    /// </summary>
    /// <returns>已载入插件的 <see cref="PluginInfo"/></returns>
    PluginInfo[] GetLoadedPluginInfos();

    /// <summary>
    /// 获取已载入的指令信息
    /// </summary>
    /// <returns>已载入插件的 <see cref="CommandInfo"/></returns>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    CommandInfo[] GetCommandInfos();
    
    /// <summary>
    /// 获取已载入的指令清单
    /// </summary>
    /// <returns>已载入插件的 <see cref="CommandManifest"/></returns>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    CommandManifest[] GetCommandManifests();

    /// <summary>
    /// 载入插件包
    /// </summary>
    /// <param name="bundle">插件包</param>
    /// <returns></returns>
    Task LoadPlugin(FileInfo bundle);
    
    /// <summary>
    /// 载入插件目录下的所有插件包
    /// </summary>
    /// <returns></returns>
    Task LoadPlugins();

    /// <summary>
    /// 卸载插件包
    /// </summary>
    /// <param name="pluginIdentifier"></param>
    /// <returns></returns>
    bool UnloadPlugin(string pluginIdentifier);

    /// <summary>
    /// 卸载所有插件
    /// </summary>
    /// <returns></returns>
    void UnloadPlugins();
}
