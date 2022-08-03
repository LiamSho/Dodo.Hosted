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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
using DodoHosted.Base;
using DodoHosted.Lib.Plugin.Exceptions;
using DodoHosted.Lib.Plugin.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.Plugin;

/// <inheritdoc />
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    
    private readonly ConcurrentDictionary<string, PluginManifest> _plugins;
    
    private readonly DirectoryInfo _pluginCacheDirectory;
    private readonly DirectoryInfo _pluginDirectory;

    public PluginManager(ILogger<PluginManager> logger)
    {
        _logger = logger;
        
        _pluginCacheDirectory = new DirectoryInfo(HostEnvs.PluginCacheDirectory);
        _pluginDirectory = new DirectoryInfo(HostEnvs.PluginDirectory);
        _plugins = new ConcurrentDictionary<string, PluginManifest>();
    }

    /// <inheritdoc />
    public IEnumerable<PluginInfo> GetLoadedPluginInfos()
    {
        return _plugins.Select(x => x.Value.PluginInfo);
    }
    
    /// <inheritdoc />
    public async Task LoadPlugin(FileInfo bundle)
    {
        try
        {
            // 检查插件包是否存在
            if (bundle.Exists is false)
            {
                throw new FileNotFoundException("找不到插件包", bundle.Name);
            }

            // 读取和解析 plugin.json 文件
            await using var fs = bundle.OpenRead();
            using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);
        
            var pluginInfoFileEntry = zipArchive.Entries.FirstOrDefault(x => x.Name == "plugin.json");

            if (pluginInfoFileEntry is null)
            {
                throw new InvalidPluginBundleException(bundle.Name, "找不到 plugin.json");
            }

            await using var pluginInfoReader = pluginInfoFileEntry.Open();

            var pluginInfo = await JsonSerializer.DeserializeAsync<PluginInfo>(pluginInfoReader);

            if (pluginInfo is null)
            {
                throw new InvalidPluginBundleException(bundle.Name, "无法解析 plugin.json");
            }
            _logger.LogTrace("已载入插件 {TracePluginBundleName} 信息 {TracePluginInfoDeserialized}", bundle.Name, pluginInfo);

            // 检查是否已有相同 Identifier 的插件
            var existingPlugin = _plugins.FirstOrDefault(x => x.Key == pluginInfo.Identifier).Value;
            if (existingPlugin is not null)
            {
                throw new PluginAlreadyLoadedException(existingPlugin.PluginInfo, pluginInfo);
            }
            _logger.LogTrace("未找到相同 Identifier 的插件 {TracePluginInfoIdentifier}", pluginInfo.Identifier);

            // 解压插件包
            var pluginCacheDirectoryPath = Path.Combine(_pluginCacheDirectory.FullName, pluginInfo.Identifier);
            var pluginCacheDirectory = new DirectoryInfo(pluginCacheDirectoryPath);

            if (pluginCacheDirectory.Exists)
            {
                pluginCacheDirectory.Delete(true);
                pluginCacheDirectory.Create();
                _logger.LogTrace("已删除已存在的插件缓存目录 {TracePluginCacheDirectoryPath}", pluginCacheDirectoryPath);
            }
            
            ZipFile.ExtractToDirectory(bundle.FullName, pluginCacheDirectory.FullName);
            _logger.LogTrace("已解压插件包 {TracePluginBundleName} 到 {TracePluginCacheDirectoryPath}", bundle.Name, pluginCacheDirectoryPath);

            // 载入程序集
            var entryAssembly = pluginCacheDirectory
                .GetFiles($"{pluginInfo.EntryAssembly}.dll", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (entryAssembly is null)
            {
                throw new InvalidPluginBundleException(bundle.Name, $"找不到 {pluginInfo.EntryAssembly}.dll");
            }
            _logger.LogTrace("找到插件程序集 {TracePluginAssemblyPath}", entryAssembly.FullName);

            var context = new PluginAssemblyLoadContext(pluginCacheDirectory.FullName);
            var assembly = context.LoadFromAssemblyPath(entryAssembly.FullName);
            _logger.LogTrace("已载入插件程序集 {TracePluginAssemblyName}", assembly.FullName);

            // 载入事件处理器
            var pluginAssemblyTypes = assembly.GetTypes();

            var eventHandlers = FetchEventHandlers(pluginAssemblyTypes);
            
            // 添加插件
            var pluginManifest = new PluginManifest
            {
                PluginEntryAssembly = assembly,
                Context = context,
                PluginInfo = pluginInfo,
                EventHandlers = eventHandlers.ToArray()
            };
            var success = _plugins.TryAdd(pluginInfo.Identifier, pluginManifest);
            Debug.Assert(success);
            
            _logger.LogInformation("已载入插件 {PluginInfo}，事件处理器 {EventHandlerCount} 个", pluginInfo, pluginManifest.EventHandlers.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "插件包 {PluginBundleName} 载入失败，{ExceptionMessage}", bundle.Name, ex.Message);
        }
    }
    
    /// <inheritdoc />
    public async Task LoadPlugins()
    {
        var bundles = _pluginDirectory.GetFiles("*.zip", SearchOption.TopDirectoryOnly);

        foreach (var bundle in bundles)
        {
            await LoadPlugin(bundle);
        }
    }

    /// <inheritdoc />
    public bool UnloadPlugin(string pluginIdentifier)
    {
        var _ = _plugins.TryRemove(pluginIdentifier, out var pluginManifest);
        pluginManifest?.Context.Unload();
        
        GC.Collect();

        return _plugins.ContainsKey(pluginIdentifier) is false;
    }

    /// <inheritdoc />
    public void UnloadPlugins()
    {
        var pluginManifests = _plugins.Values.ToList();
        _plugins.Clear();
        
        foreach (var pluginManifest in pluginManifests)
        {
            pluginManifest.Context.Unload();
        }
        
        GC.Collect();
    }

    /// <summary>
    /// 从 Plugin Assembly 中载入所有的事件处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns><see cref="EventHandlerManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private static IEnumerable<EventHandlerManifest> FetchEventHandlers(IEnumerable<Type> types)
    {
        var eventHandlerTypes = types
            .Where(x => x != typeof(IDodoHostedPluginEventHandler<>))
            .Where(x => x
                .GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDodoHostedPluginEventHandler<>)))
            .Where(x => x.ContainsGenericParameters is false);

        var manifests = new List<EventHandlerManifest>();
        foreach (var type in eventHandlerTypes)
        {
            var interfaceType = type.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDodoHostedPluginEventHandler<>));
                
            var handler = Activator.CreateInstance(type);
            var method = interfaceType.GetMethod("Handle");
            var eventType = interfaceType.GetGenericArguments().FirstOrDefault();

            if (handler is null)
            {
                throw new PluginAssemblyLoadException($"无法创建插件事件处理器 {type.FullName} 的实例");
            }
            if (method is null)
            {
                throw new PluginAssemblyLoadException($"找不到到插件事件处理器 {type.FullName} 的 Handle 方法");
            }
            if (eventType is null)
            {
                throw new PluginAssemblyLoadException($"找不到到插件事件处理器 {type.FullName} 的事件类型");
            }
            
            manifests.Add(new EventHandlerManifest
            {
                EventHandler = handler,
                EventType = eventType,
                EventHandlerType = type,
                HandlerMethod = method
            });
        }

        return manifests;
    }
}
 
