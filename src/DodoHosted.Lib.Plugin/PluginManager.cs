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
using System.Reflection;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Web;
using DodoHosted.Base.Events;
using DodoHosted.Lib.Plugin.Exceptions;
using DodoHosted.Lib.Plugin.Models;
using DodoHosted.Lib.SdkWrapper;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.Plugin;

/// <inheritdoc />
public partial class PluginManager : IPluginManager
{
    private readonly ILogger _logger;
    private readonly ILogger _pluginLifetimeLogger;
    private readonly ILogger _eventHandlerLogger;
    private readonly ILogger _pluginHostedServiceLogger;
    
    private readonly IChannelLogger _channelLogger;
    private readonly IServiceProvider _provider;
    private readonly OpenApiService _openApiService;

    private readonly ConcurrentDictionary<string, PluginManifest> _plugins;
    
    private readonly DirectoryInfo _pluginCacheDirectory;
    private readonly DirectoryInfo _pluginDirectory;

    private IEnumerable<CommandManifest> AllCommands => _plugins.IsEmpty
        ? _nativeCommandExecutors
        : _plugins.Values
            .SelectMany(x => x.CommandManifests)
            .Concat(_nativeCommandExecutors)
            .ToArray();

    private IEnumerable<EventHandlerManifest> AllEventHandlers => _plugins.IsEmpty
        ? _nativeEventHandlers
        : _plugins.Values.SelectMany(x => x.EventHandlers).Concat(_nativeEventHandlers);
    private IEnumerable<EventHandlerManifest> NativeEventHandlers => _nativeEventHandlers;
    private IEnumerable<EventHandlerManifest> SpecificEventHandlers(string identifier) =>
        _plugins.Values.FirstOrDefault(x => x.PluginInfo.Identifier == identifier)?.EventHandlers
        ?? Enumerable.Empty<EventHandlerManifest>();

    // ReSharper disable once CollectionNeverUpdated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public static List<Assembly> NativeAssemblies { get; } = new();

    private readonly Dictionary<IPluginLifetime, IServiceScope> _nativePluginLifetimes = new();
    private readonly List<CommandManifest> _nativeCommandExecutors = new();
    private readonly List<EventHandlerManifest> _nativeEventHandlers = new();
    private readonly List<HostedServiceManifest> _nativeHostedServices = new();

    public PluginManager(
        ILoggerFactory loggerFactory,
        IChannelLogger channelLogger,
        IServiceProvider provider,
        OpenApiService openApiService)
    {
        _logger = loggerFactory.CreateLogger("PluginManager");
        _pluginLifetimeLogger = loggerFactory.CreateLogger("PluginLifetime");
        _eventHandlerLogger = loggerFactory.CreateLogger("EventHandler");
        _pluginHostedServiceLogger = loggerFactory.CreateLogger("PluginHostedService");
        
        _channelLogger = channelLogger;
        _provider = provider;
        _openApiService = openApiService;

        _pluginCacheDirectory = new DirectoryInfo(HostEnvs.PluginCacheDirectory);
        _pluginDirectory = new DirectoryInfo(HostEnvs.PluginDirectory);
        _plugins = new ConcurrentDictionary<string, PluginManifest>();

        if (_pluginDirectory.Exists is false)
        {
            _pluginDirectory.Create();
        }
        if (_pluginCacheDirectory.Exists is false)
        {
            _pluginCacheDirectory.Create();
        }

        var webRequestTypeFullName = typeof(DodoHostedWebRequestEvent).FullName ?? string.Empty;
        
        DodoEventProcessor.DodoEvent += EventListener;
        PluginSystemController.PluginWebOperationEvent += async (identifier, island, body) =>
        {
            await RunEvent(new DodoHostedWebRequestEvent(island, body), webRequestTypeFullName, identifier == "*" ? null : identifier);
        };
        
        NativeAssemblies.Add(this.GetType().Assembly);
    }

    /// <inheritdoc />
    // ReSharper disable once ReturnTypeCanBeNotNullable
    public PluginManifest? GetPluginManifest(string pluginIdentifier)
    {
        var kv = _plugins.FirstOrDefault(x => x.Key == pluginIdentifier);
        return kv.Value;
    }

    /// <inheritdoc />
    public IEnumerable<PluginInfo> GetLoadedPluginInfos()
    {
        return _plugins.Select(x => x.Value.PluginInfo);
    }

    /// <inheritdoc />
    public async Task<(Dictionary<PluginInfo, string>, List<string>)> GetAllPluginInfos()
    {
        var result = new Dictionary<PluginInfo, string>();
        var failed = new List<string>();
        
        var bundles = _pluginDirectory.GetFiles("*.zip", SearchOption.TopDirectoryOnly);
        foreach (var fileInfo in bundles)
        {
            var pluginInfo = await PluginLoadHelper.ReadPluginInfo(fileInfo);

            if (pluginInfo is null)
            {
                failed.Add(fileInfo.Name);
                continue;
            }

            var enabled = _plugins.Any(x => x.Key == pluginInfo.Identifier);
            result.Add(pluginInfo, enabled ? string.Empty : fileInfo.Name);
        }

        return (result, failed);
    }

    /// <inheritdoc />
    public IEnumerable<CommandManifest> GetCommandManifests()
    {
        return AllCommands;
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
            var pluginInfo = await PluginLoadHelper.ReadPluginInfo(bundle);

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

            var pluginAssemblyTypes = assembly.GetTypes();

            // 载入事件处理器
            var eventHandlers = pluginAssemblyTypes.FetchEventHandlers(_logger);
            
            // 载入指令处理器
            var commandExecutors = pluginAssemblyTypes.FetchCommandExecutors(_logger, _provider);
            
            // 载入后台服务
            var hostedServices = pluginAssemblyTypes.FetchHostedService(_logger, _provider).ToArray();
            
            // 载入插件生命周期类
            var pluginLifetime = pluginAssemblyTypes.FetchPluginLifetime();

            var scope = _provider.CreateScope();
            
            if (pluginLifetime is not null)
            {
                await pluginLifetime.Load(scope.ServiceProvider, _pluginLifetimeLogger);
            }
            
            // 运行所有的后台服务
            foreach (var service in hostedServices)
            {
                _logger.LogInformation("开始执行插件后台任务 {PluginHostedServiceName}", service.Name);
                service.JobTask = service.Job.StartAsync(
                    service.Scope.ServiceProvider,
                    _pluginHostedServiceLogger,
                    service.CancellationTokenSource.Token);
            }
            
            // 添加插件
            var pluginManifest = new PluginManifest
            {
                PluginEntryAssembly = assembly,
                Context = context,
                PluginInfo = pluginInfo,
                PluginScope = scope,
                PluginLifetime = pluginLifetime,
                EventHandlers = eventHandlers.ToArray(),
                CommandManifests = commandExecutors.ToArray(),
                HostedServices = hostedServices
            };
            var success = _plugins.TryAdd(pluginInfo.Identifier, pluginManifest);
            Debug.Assert(success);
            
            _logger.LogInformation("已载入插件 {PluginInfo}，事件处理器 {EventHandlerCount} 个，指令 {CommandCount} 个，后台任务 {HostedServiceCount} 个",
                pluginInfo, pluginManifest.EventHandlers.Length, pluginManifest.CommandManifests.Length, pluginManifest.HostedServices.Length);
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
        var _ = _plugins.TryRemove(pluginIdentifier, out var pluginManifest);
        
        pluginManifest?.HostedServices.AsParallel()
            .ForAll(x =>
            {
                x.CancellationTokenSource.Cancel();
                try
                {
                    x.JobTask?.Wait();
                }
                catch (AggregateException ex)
                {
                    if (ex.InnerExceptions.FirstOrDefault() is not TaskCanceledException)
                    {
                        _logger.LogError(ex, "取消 Task 出现异常");
                    }
                }
                x.Scope.Dispose();
                _logger.LogInformation("停止插件后台任务 {PluginHostedServiceName}", x.Name);
            });
        pluginManifest?.PluginLifetime?.Unload(_pluginLifetimeLogger).GetAwaiter().GetResult();
        pluginManifest?.PluginScope.Dispose();
        pluginManifest?.Context.Unload();
        
        GC.Collect();
        
        var status = _plugins.ContainsKey(pluginIdentifier) is false;
        
        _logger.Log(status ? LogLevel.Information : LogLevel.Warning,
            "插件 {PluginUnloadIdentifier} 卸载任务完成，{PluginUnloadStatus}",
            pluginIdentifier, status ? "成功" : "失败");

        return status;
    }

    /// <inheritdoc />
    public void UnloadPlugins()
    {
        _logger.LogInformation("执行卸载所有插件任务");
        
        var pluginManifests = _plugins.Values.ToList();
        _plugins.Clear();

        foreach (var pluginManifest in pluginManifests)
        {
            pluginManifest.HostedServices.AsParallel()
                .ForAll(x =>
                {
                    x.CancellationTokenSource.Cancel();
                    try
                    {
                        x.JobTask?.Wait();
                    }
                    catch (AggregateException ex)
                    {
                        if (ex.InnerExceptions.FirstOrDefault() is not TaskCanceledException)
                        {
                            _logger.LogError(ex, "取消 Task 出现异常");
                        }
                    }
                    x.Scope.Dispose();
                    _logger.LogInformation("停止插件后台任务 {PluginHostedServiceName}", x.Name);
                });
            pluginManifest.PluginLifetime?.Unload(_pluginLifetimeLogger).GetAwaiter().GetResult();
            pluginManifest.PluginScope.Dispose();
            pluginManifest.Context.Unload();
        }
        
        GC.Collect();
        
        _logger.LogInformation("卸载所有插件任务已完成，当前插件数量：{PluginsCount}", _plugins.Count);
    }

    /// <inheritdoc />
    public async Task LoadNativeTypes()
    {
        foreach (var assembly in NativeAssemblies)
        {
            _logger.LogDebug("载入 Native 程序集 {DbgNativeAssemblyName}", assembly.FullName);

            var types = assembly.GetTypes();
            
            // 载入事件处理器
            var eventHandlers = types.FetchEventHandlers(_logger);
            
            // 载入指令处理器
            var commandExecutors = types.FetchCommandExecutors(_logger, _provider);
            
            // 载入后台服务
            var hostedServices = types.FetchHostedService(_logger, _provider).ToArray();
            
            // 载入插件生命周期类
            var pluginLifetime = types.FetchPluginLifetime();

            var scope = _provider.CreateScope();

            if (pluginLifetime is not null)
            {
                await pluginLifetime.Load(scope.ServiceProvider, _pluginLifetimeLogger);
                _nativePluginLifetimes.Add(pluginLifetime, scope);
            }

            foreach (var service in hostedServices)
            {
                _logger.LogInformation("开始执行 Native Assembly 后台任务 {NativeAssemblyHostedServiceName}", service.Name);
                service.JobTask = service.Job.StartAsync(
                    service.Scope.ServiceProvider,
                    _pluginHostedServiceLogger,
                    service.CancellationTokenSource.Token);
            }
            
            _nativeCommandExecutors.AddRange(commandExecutors);
            _nativeEventHandlers.AddRange(eventHandlers);
            _nativeHostedServices.AddRange(hostedServices);
            
            _logger.LogInformation("已载入 Native Assembly: {NativeAssemblyName}", assembly.FullName);
        }
    }

    /// <inheritdoc />
    public void UnloadNativeTypes()
    {
        foreach (var hostedService in _nativeHostedServices)
        {
            hostedService.CancellationTokenSource.Cancel();
            try
            {
                hostedService.JobTask?.Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.FirstOrDefault() is not TaskCanceledException)
                {
                    _logger.LogError(ex, "取消 Task 出现异常");
                }
            }
            hostedService.Scope.Dispose();
            _logger.LogInformation("停止 Native Assembly 后台任务 {NativeAssemblyHostedServiceName}", hostedService.Name);
        }
        
        foreach (var (nativePluginLifetime, scope) in _nativePluginLifetimes)
        {
            nativePluginLifetime.Unload(_pluginLifetimeLogger);
            scope.Dispose();
        }
    }
}
