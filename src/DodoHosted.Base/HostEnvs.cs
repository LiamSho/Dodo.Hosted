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

using System.Reflection;

namespace DodoHosted.Base;

/// <summary>
/// Host 变量
/// </summary>
public static class HostEnvs
{
    /// <summary>
    /// 插件载入缓存目录
    /// </summary>
    public static string PluginCacheDirectory => ReadEnvironmentVariable(
        "DODO_HOSTED_PLUGIN_CACHE_DIRECTORY",
        Path.Combine(AssemblyDirectory, "PluginCache"));

    /// <summary>
    /// 插件目录
    /// </summary>
    public static string PluginDirectory => ReadEnvironmentVariable(
            "DODO_HOSTED_PLUGIN_DIRECTORY",
            Path.Combine(AssemblyDirectory, "Plugin"));

    /// <summary>
    /// Dodo SDK API 终结点
    /// </summary>
    public static string DodoSdkApiEndpoint => ReadEnvironmentVariable(
        "DODO_SDK_API_ENDPOINT",
        "https://botopen.imdodo.com");
    
    /// <summary>
    /// 入口 Assembly 目录
    /// </summary>
    public static string AssemblyDirectory => new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName;

    private static string ReadEnvironmentVariable(string key, string defaultValue) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))
            ? defaultValue
            : Environment.GetEnvironmentVariable(key)!;
}
