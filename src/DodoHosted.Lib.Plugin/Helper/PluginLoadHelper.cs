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

using System.IO.Compression;
using System.Text.Json;

namespace DodoHosted.Lib.Plugin.Helper;

internal static class PluginLoadHelper
{
    /// <summary>
    /// 解压插件包
    /// </summary>
    /// <param name="bundle"></param>
    /// <param name="pluginInfo"></param>
    /// <param name="targetDirectory"></param>
    internal static DirectoryInfo ExtractPluginBundle(this FileInfo bundle, PluginInfo pluginInfo, DirectoryInfo targetDirectory)
    {
        var pluginCacheDirectoryPath = Path.Combine(targetDirectory.FullName, pluginInfo.Identifier);
        var pluginCacheDirectory = new DirectoryInfo(pluginCacheDirectoryPath);

        if (pluginCacheDirectory.Exists)
        {
            pluginCacheDirectory.Delete(true);
            pluginCacheDirectory.Create();
        }

        ZipFile.ExtractToDirectory(bundle.FullName, pluginCacheDirectory.FullName);

        return pluginCacheDirectory;
    }

    /// <summary>
    /// 读取插件包的 <c>plugin.json</c>
    /// </summary>
    /// <param name="bundle"></param>
    /// <returns></returns>
    /// <exception cref="FileNotFoundException"></exception>
    internal static async Task<PluginInfo> ReadPluginInfo(this FileInfo bundle)
    {
        // 检查插件包是否存在
        if (bundle.Exists is false)
        {
            throw new FileNotFoundException("找不到插件包", bundle.Name);
        }
        
        await using var fs = bundle.OpenRead();
        using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);
        
        var pluginInfoFileEntry = zipArchive.Entries.FirstOrDefault(x => x.Name == "plugin.json");
        if (pluginInfoFileEntry is null)
        {
            throw new PluginAssemblyLoadException("找不到 plugin.json");
        }
            
        await using var pluginInfoReader = pluginInfoFileEntry.Open();

        var pluginInfo = await JsonSerializer.DeserializeAsync<PluginInfo>(pluginInfoReader);
        if (pluginInfo is null)
        {
            throw new PluginAssemblyLoadException($"插件包 {bundle.FullName} 中找不到 plugin.json");
        }

        var apiVersion = pluginInfo.ApiVersion;
        var isCompatible = PluginApiLevel.IsCompatible(apiVersion);
        
        if (isCompatible is false)
        {
            throw new PluginAssemblyLoadException($"插件包 {bundle.FullName} 的 API 版本 {apiVersion} 与主程序不兼容，需求 {PluginApiLevel.GetApiLevelString()}");
        }
        
        return pluginInfo;
    }

    /// <summary>
    /// 构建 <see cref="PluginAssemblyLoadContext"/> 并加载程序集
    /// </summary>
    /// <param name="source"></param>
    /// <param name="pluginInfo"></param>
    /// <returns></returns>
    internal static (PluginAssemblyLoadContext, Assembly[]) LoadPluginAssembly(this DirectoryInfo source, PluginInfo pluginInfo)
    {
        var entryAssembly = source
            .GetFiles($"{pluginInfo.EntryAssembly}.dll", SearchOption.TopDirectoryOnly)
            .FirstOrDefault();

        if (entryAssembly is null)
        {
            throw new PluginAssemblyLoadException($"找不到 {pluginInfo.EntryAssembly}.dll");
        }

        var context = new PluginAssemblyLoadContext(entryAssembly.FullName);
        var assembly = context.LoadFromAssemblyPath(entryAssembly.FullName);

        var refs = assembly.GetReferencedAssemblies()
            .Where(x => x.FullName.StartsWith("DodoHosted") is false);

        var assemblies = refs.Select(x => context.LoadFromAssemblyName(x)).ToList();
        assemblies.Add(assembly);

        return (context, assemblies.ToArray());
    }
}
