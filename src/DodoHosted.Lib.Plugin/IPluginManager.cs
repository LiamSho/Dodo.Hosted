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

using DoDo.Open.Sdk.Models.Messages;
using DodoHosted.Base;
using DodoHosted.Base.Events;
using DodoHosted.Lib.Plugin.Models;

namespace DodoHosted.Lib.Plugin;

/// <summary>
/// 插件管理器
/// </summary>
public interface IPluginManager
{
    /// <summary>
    /// 获取插件 Manifest
    /// </summary>
    /// <param name="pluginIdentifier">插件标识符</param>
    /// <returns></returns>
    PluginManifest? GetPluginManifest(string pluginIdentifier);
    
    /// <summary>
    /// 获取已载入插件的信息
    /// </summary>
    /// <returns>已载入插件的 <see cref="PluginInfo"/></returns>
    PluginInfo[] GetLoadedPluginInfos();

    /// <summary>
    /// 获取所有插件的信息
    /// </summary>
    /// <returns>
    /// 插件信息 <see cref="PluginInfo"/> 与插件载入状态组成的字典，若值为空，表示启用，否则为插件包文件名，
    /// 获取信息失败的插件为 Tuple 第二个值
    /// </returns>
    Task<(Dictionary<PluginInfo, string>, List<string>)> GetAllPluginInfos();

    /// <summary>
    /// 获取已载入的指令信息
    /// </summary>
    /// <returns>已载入插件的 <see cref="CommandInfo"/></returns>
    // ReSharper disable once ReturnTypeCanBeEnumerable.Global
    CommandInfo[] GetCommandInfos();
    
    /// <summary>
    /// 载入插件包
    /// </summary>
    /// <param name="bundle">插件包</param>
    /// <returns></returns>
    Task LoadPlugin(FileInfo bundle);
    
    /// <summary>
    /// 载入插件包
    /// </summary>
    /// <param name="bundle">插件包</param>
    /// <returns></returns>
    Task LoadPlugin(string bundle);
    
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

    /// <summary>
    /// 卸载本地类型
    /// </summary>
    void UnloadNativeTypes();
    
    /// <summary>
    /// 加载本地类型
    /// </summary>
    Task LoadNativeTypes();

    /// <summary>
    /// 执行指令
    /// </summary>
    /// <param name="messageEvent">消息事件</param>
    /// <returns></returns>
    Task RunCommand(DodoChannelMessageEvent<MessageBodyText> messageEvent);

    /// <summary>
    /// 运行事件处理器
    /// </summary>
    /// <param name="event">事件消息</param>
    /// <param name="typeString">类型字符串</param>
    /// <returns></returns>
    Task<int> RunEvent(IDodoHostedEvent @event, string typeString);
}
