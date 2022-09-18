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

namespace DodoHosted.Lib.Plugin.Services;

public class PluginLifetimeManager : IPluginLifetimeManager
{
    private readonly ILogger<PluginLifetimeManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly IPluginManager _pluginManager;

    private readonly DirectoryInfo _pluginCacheDirectory;
    private readonly DirectoryInfo _pluginDirectory;

    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly List<Assembly> NativeAssemblies = new();

    public PluginLifetimeManager(
        ILogger<PluginLifetimeManager> logger,
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
            var pluginAssemblyTypes = assemblies.SelectMany(x => x.GetTypes()).ToArray();

            // 载入插件实例
            var pluginInstance = pluginAssemblyTypes.FetchPluginInstance();
            if (pluginInstance is null)
            {
                throw new PluginAssemblyLoadException($"标识符为 {pluginInfo.Identifier} 的插件实例为空");
            }

            await pluginInstance.OnLoad();
            
            // 载入插件工作类型
            var worker = pluginAssemblyTypes.LoadPluginWorkers(_provider);

            // 添加插件
            var pluginManifest = new PluginManifest
            {
                PluginAssemblies = assemblies,
                Context = context,
                PluginInfo = pluginInfo,
                IsNative = false,
                PluginScope = _provider.CreateScope(),
                DodoHostedPlugin = pluginInstance,
                Worker = worker
            };

            var success = _pluginManager.AddPlugin(pluginManifest);

            if (success is false)
            {
                pluginManifest.UnloadPlugin(_logger, true);
                throw new PluginAssemblyLoadException($"插件 {pluginInfo.Identifier} 添加失败");
            }
            
            _logger.LogInformation("已载入插件 {PluginInfo}，事件处理器 {EventHandlerCount} 个，指令 {CommandCount} 个，后台任务 {HostedServiceCount} 个",
                pluginInfo, worker.EventHandlers.Length, worker.CommandExecutors.Length, worker.HostedServices.Length);
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

        var manifest = _pluginManager.RemovePlugin(pluginIdentifier);
        var status = manifest is not null;
        
        manifest?.UnloadPlugin(_logger);
        
        _logger.Log(status ? LogLevel.Information : LogLevel.Warning,
            "插件 {PluginUnloadIdentifier} 卸载任务完成，{PluginUnloadStatus}",
            pluginIdentifier, status ? "成功" : "失败");

        return status;
    }

    /// <inheritdoc />
    public void UnloadPlugins()
    {
        _logger.LogInformation("执行卸载所有插件任务");

        var manifests = _pluginManager.RemovePlugins();
        manifests.AsParallel().ForAll(x => x.UnloadPlugin(_logger));
        
        _logger.LogInformation("卸载所有插件任务已完成");
    }
    
    /// <inheritdoc />
    public async Task LoadNativeTypes()
    {
        foreach (var assembly in NativeAssemblies)
        {
            _logger.LogDebug("载入 Native 程序集 {DbgNativeAssemblyName}", assembly.FullName);

            var name = assembly.GetName().Name;
            
            var types = assembly.GetTypes();

            var instance = types.FetchPluginInstance() ?? new DefaultPluginInstance();

            await instance.OnLoad();
            
            var worker = types.LoadPluginWorkers(_provider, true);

            var scope = _provider.CreateScope();

            var manifest = new PluginManifest
            {
                PluginInfo = new PluginInfo
                {
                    Name = $"native-{name}",
                    Author = "Native",
                    Description = "Native Assembly",
                    EntryAssembly = assembly.FullName!,
                    Identifier = $"native-{name}",
                    Version = "native",
                    ApiVersion = PluginApiLevel.CurrentApiLevel
                },
                Context = null,
                IsNative = true,
                DodoHostedPlugin = instance,
                PluginAssemblies = new []{ assembly },
                PluginScope = scope,
                Worker = worker
            };

            var success = _pluginManager.AddPlugin(manifest);

            if (success is false)
            {
                throw new PluginAssemblyLoadException($"Native 类型 {name} 添加失败");
            }
            
            _logger.LogInformation("已载入 Native Assembly: {NativeAssemblyName}，事件处理器 {EventHandlerCount} 个，指令 {CommandCount} 个，后台任务 {HostedServiceCount} 个", 
                assembly.FullName, worker.EventHandlers.Length, worker.CommandExecutors.Length, worker.HostedServices.Length);
        }
    }

    /// <inheritdoc />
    public void UnloadNativeTypes()
    {
        var nativeManifests = _pluginManager.GetPlugins(true);

        foreach (var nativeManifest in nativeManifests)
        {
            nativeManifest.UnloadPlugin(_logger);
        }
    }
}
