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

using DodoHosted.Lib.Plugin.Models.Module;

namespace DodoHosted.Lib.Plugin.Services;

public class PluginLoadingManager : IPluginLoadingManager
{
    private readonly ILogger<PluginLoadingManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly IPluginManager _pluginManager;

    private readonly DirectoryInfo _pluginCacheDirectory;
    private readonly DirectoryInfo _pluginDirectory;

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly List<Assembly> NativeAssemblies = new();

    public PluginLoadingManager(
        ILogger<PluginLoadingManager> logger,
        IServiceProvider provider,
        IPluginManager pluginManager)
    {
        _logger = logger;
        _provider = provider;
        _pluginManager = pluginManager;

        _pluginCacheDirectory = new DirectoryInfo(HostEnvs.PluginCacheDirectory);
        _pluginDirectory = new DirectoryInfo(HostEnvs.PluginDirectory);
        
        if (_pluginDirectory.Exists is false)
        {
            _pluginDirectory.Create();
        }
        if (_pluginCacheDirectory.Exists is false)
        {
            _pluginCacheDirectory.Create();
        }
        
        NativeAssemblies.Add(this.GetType().Assembly);
    }
    
    /// <inheritdoc />
    public async Task LoadPlugin(FileInfo bundle)
    {
        try
        {
            // 读取插件信息
            var pluginInfo = await bundle.ReadPluginInfo();
            
            // 检查插件标识符是否有重复
            if (_pluginManager.Exist(pluginInfo.Identifier))
            {
                throw new PluginAssemblyLoadException($"标识符为 {pluginInfo.Identifier} 的插件已存在");
            }
            
            // 解压缩
            var pluginCacheDirectory = bundle.ExtractPluginBundle(pluginInfo, _pluginCacheDirectory);
            
            // 载入程序集
            var (context, assemblies) = pluginCacheDirectory.LoadPluginAssembly(pluginInfo);

            // 插件程序集类型
            var module = new PluginModule(context, assemblies, pluginInfo, _provider, bundle.FullName);

            var success = _pluginManager.AddPlugin(module);

            if (success is false)
            {
                throw new PluginAssemblyLoadException($"插件 {pluginInfo.Identifier} 添加失败");
            }
            
            _logger.LogInformation("已载入插件 {PluginInfo}，" +
                                   "事件处理器 {EventHandlerCount} 个，" +
                                   "Web 事件处理器 {WebEventHandler} 个，" +
                                   "指令执行器 {CommandCount} 个，" +
                                   "后台任务 {HostedServiceCount} 个",
                pluginInfo,
                module.EventHandlerModule.Count(),
                module.WebHandlerModule.Count(),
                module.CommandExecutorModule.Count(),
                module.HostedServiceModule.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "插件包 {PluginBundleName} 载入失败，{ExceptionMessage}", bundle.Name, ex.Message);
        }
    }

    /// <inheritdoc />
    public async Task LoadPlugin(string bundle)
    {
        var fileInfo = _pluginDirectory
            .GetFiles("*.zip", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(x => x.Name == bundle);
        if (fileInfo is null)
        {
            return;
        }

        await LoadPlugin(fileInfo);
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
        _logger.LogInformation("执行卸载插件 {PluginUnloadIdentifier} 任务", pluginIdentifier);

        var module = _pluginManager.RemovePlugin(pluginIdentifier);
        var status = module is not null;
        
        module?.Unload();
        
        _logger.Log(status ? LogLevel.Information : LogLevel.Warning,
            "插件 {PluginUnloadIdentifier} 卸载任务完成，{PluginUnloadStatus}",
            pluginIdentifier, status ? "成功" : "失败");

        return status;
    }

    /// <inheritdoc />
    public void UnloadPlugins()
    {
        _logger.LogInformation("执行卸载所有插件任务");

        var modules = _pluginManager.RemovePlugins();
        modules.AsParallel().ForAll(x => x.Unload());
        
        _logger.LogInformation("卸载所有插件任务已完成");
    }
    
    /// <inheritdoc />
    public void LoadNativeTypes()
    {
        foreach (var assembly in NativeAssemblies)
        {
            _logger.LogDebug("载入 Native 程序集 {DbgNativeAssemblyName}", assembly.FullName);

            var name = assembly.FullName ?? string.Empty;

            var pluginInfo = new PluginInfo
            {
                Name = $"native-{name}",
                Author = "Native",
                Description = "Native Assembly",
                EntryAssembly = assembly.FullName!,
                Identifier = $"native-{name}",
                Version = "native",
                ApiVersion = PluginApiLevel.CurrentApiLevel
            };
            
            var module = new PluginModule(null, new[] { assembly }, pluginInfo, _provider, string.Empty, true);

            var success = _pluginManager.AddPlugin(module);

            if (success is false)
            {
                throw new PluginAssemblyLoadException($"Native 类型 {name} 添加失败");
            }
            
            _logger.LogInformation("已载入Native Assembly {PluginIdentifier}，" +
                                   "事件处理器 {EventHandlerCount} 个，" +
                                   "Web 事件处理器 {WebEventHandler} 个，" +
                                   "指令执行器 {CommandCount} 个，" +
                                   "后台任务 {HostedServiceCount} 个",
                pluginInfo,
                module.EventHandlerModule.Count(),
                module.WebHandlerModule.Count(),
                module.CommandExecutorModule.Count(),
                module.HostedServiceModule.Count());
        }
    }

    /// <inheritdoc />
    public void UnloadNativeTypes()
    {
        var nativeModules = _pluginManager.GetPlugins(true);

        foreach (var nativeModule in nativeModules)
        {
            nativeModule.Unload();
        }
    }

    public async Task<(Dictionary<string, PluginInfo>, Dictionary<string, Exception>)> GetUnloadedPlugins()
    {
        var bundles = _pluginDirectory.GetFiles("*.zip", SearchOption.TopDirectoryOnly);
        var loadedBundles = _pluginManager
            .GetPlugins(x => x.IsNative is false)
            .Select(x => x.BundlePath);

        var unloadedBundles = bundles
            .Select(x => x.FullName)
            .Except(loadedBundles)
            .Select(x => new FileInfo(x));

        var unloadedPluginInfos = new Dictionary<string, PluginInfo>();
        var failedReadPluginInfos = new Dictionary<string, Exception>();

        foreach (var bundle in unloadedBundles)
        {
            try
            {
                var pluginInfo = await bundle.ReadPluginInfo();
                unloadedPluginInfos.Add(bundle.Name, pluginInfo);
            }
            catch (Exception ex)
            {
                failedReadPluginInfos.Add(bundle.Name, ex);
            }
        }
        
        return (unloadedPluginInfos, failedReadPluginInfos);
    }
}
