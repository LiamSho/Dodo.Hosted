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
using System.Text;
using System.Text.Json;
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
    private readonly ILogger<PluginManager> _logger;
    private readonly ILogger _pluginLifetimeLogger;
    private readonly ILogger _eventHandlerLogger;
    private readonly IChannelLogger _channelLogger;
    private readonly IServiceProvider _provider;
    private readonly OpenApiService _openApiService;

    private readonly ConcurrentDictionary<string, PluginManifest> _plugins;
    
    private readonly DirectoryInfo _pluginCacheDirectory;
    private readonly DirectoryInfo _pluginDirectory;

    private readonly CommandManifest[] _builtinCommands;

    private IEnumerable<CommandManifest> AllCommands => _plugins.IsEmpty
        ? _builtinCommands.Concat(_nativeCommandExecutors)
        : _plugins.Values
            .Select(x => x.CommandManifests)
            .Aggregate((x, y) => x.Concat(y).ToArray())
            .Concat(_builtinCommands)
            .Concat(_nativeCommandExecutors)
            .ToArray();

    // ReSharper disable once CollectionNeverUpdated.Global
    // ReSharper disable once MemberCanBePrivate.Global
    public static List<Assembly> NativeAssemblies { get; } = new();

    private readonly Dictionary<IPluginLifetime, IServiceScope> _nativePluginLifetimes = new();
    private readonly List<CommandManifest> _nativeCommandExecutors = new();
    private readonly List<EventHandlerManifest> _nativeEventHandlers = new();

    public PluginManager(
        ILogger<PluginManager> logger,
        ILoggerFactory loggerFactory,
        IChannelLogger channelLogger,
        IServiceProvider provider,
        OpenApiService openApiService)
    {
        _logger = logger;
        _pluginLifetimeLogger = loggerFactory.CreateLogger("PluginLifetime");
        _eventHandlerLogger = loggerFactory.CreateLogger("EventHandler");
        _channelLogger = channelLogger;
        _provider = provider;
        _openApiService = openApiService;

        _pluginCacheDirectory = new DirectoryInfo(HostEnvs.PluginCacheDirectory);
        _pluginDirectory = new DirectoryInfo(HostEnvs.PluginDirectory);
        _plugins = new ConcurrentDictionary<string, PluginManifest>();

        _builtinCommands = FetchCommandExecutors(this.GetType().Assembly.GetTypes()).ToArray();

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
    }

    /// <inheritdoc />
    // ReSharper disable once ReturnTypeCanBeNotNullable
    public PluginManifest? GetPluginManifest(string pluginIdentifier)
    {
        var kv = _plugins.FirstOrDefault(x => x.Key == pluginIdentifier);
        return kv.Value;
    }

    /// <inheritdoc />
    public PluginInfo[] GetLoadedPluginInfos()
    {
        return _plugins.Select(x => x.Value.PluginInfo).ToArray();
    }

    /// <inheritdoc />
    public async Task<(Dictionary<PluginInfo, string>, List<string>)> GetAllPluginInfos()
    {
        var result = new Dictionary<PluginInfo, string>();
        var failed = new List<string>();
        
        var bundles = _pluginDirectory.GetFiles("*.zip", SearchOption.TopDirectoryOnly);
        foreach (var fileInfo in bundles)
        {
            var pluginInfo = await ReadPluginInfo(fileInfo);

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
    public CommandInfo[] GetCommandInfos()
    {
        return AllCommands.Select(x => x as CommandInfo).ToArray();
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
            var pluginInfo = await ReadPluginInfo(bundle);

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
            var eventHandlers = FetchEventHandlers(pluginAssemblyTypes);
            
            // 载入指令处理器
            var commandExecutors = FetchCommandExecutors(pluginAssemblyTypes);
            
            // 载入插件生命周期类
            var pluginLifetime = FetchPluginLifetime(pluginAssemblyTypes);

            var scope = _provider.CreateScope();
            
            if (pluginLifetime is not null)
            {
                await pluginLifetime.Load(scope.ServiceProvider, _pluginLifetimeLogger);
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
                CommandManifests = commandExecutors.ToArray()
            };
            var success = _plugins.TryAdd(pluginInfo.Identifier, pluginManifest);
            Debug.Assert(success);
            
            _logger.LogInformation("已载入插件 {PluginInfo}，事件处理器 {EventHandlerCount} 个，指令 {CommandCount} 个",
                pluginInfo, pluginManifest.EventHandlers.Length, pluginManifest.CommandManifests.Length);
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
        
        pluginManifest?.PluginLifetime?
            .Unload(_pluginLifetimeLogger).GetAwaiter().GetResult();
        
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
            pluginManifest.PluginLifetime?
                .Unload(_pluginLifetimeLogger).GetAwaiter().GetResult();
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
            var eventHandlers = FetchEventHandlers(types);
            
            // 载入指令处理器
            var commandExecutors = FetchCommandExecutors(types);
            
            // 载入插件生命周期类
            var pluginLifetime = FetchPluginLifetime(types);

            var scope = _provider.CreateScope();

            if (pluginLifetime is not null)
            {
                await pluginLifetime.Load(scope.ServiceProvider, _pluginLifetimeLogger);
                _nativePluginLifetimes.Add(pluginLifetime, scope);
            }
            
            _nativeCommandExecutors.AddRange(commandExecutors);
            _nativeEventHandlers.AddRange(eventHandlers);
        }
    }

    /// <inheritdoc />
    public void UnloadNativeTypes()
    {
        foreach (var (nativePluginLifetime, scope) in _nativePluginLifetimes)
        {
            nativePluginLifetime.Unload(_pluginLifetimeLogger);
            scope.Dispose();
        }
    }

    /// <summary>
    /// 从 Plugin Assembly 中载入所有的事件处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns><see cref="EventHandlerManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private IEnumerable<EventHandlerManifest> FetchEventHandlers(IEnumerable<Type> types)
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
            
            _logger.LogTrace("已载入事件处理器 {TraceLoadedEventHandler}", type.FullName);
            
            manifests.Add(new EventHandlerManifest
            {
                EventHandler = handler,
                EventType = eventType,
                EventTypeString = eventType.FullName!,
                EventHandlerType = type,
                HandlerMethod = method
            });
        }

        return manifests;
    }

    /// <summary>
    /// 从 Plugin Assembly 中载入所有的指令处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns><see cref="CommandManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private IEnumerable<CommandManifest> FetchCommandExecutors(IEnumerable<Type> types)
    {
        var commandExecutorTypes = types
            .Where(x => x != typeof(ICommandExecutor))
            .Where(x => x.IsAssignableTo(typeof(ICommandExecutor)))
            .Where(x => x.ContainsGenericParameters is false)
            .ToList();
        
        var manifests = new List<CommandManifest>();
        foreach (var type in commandExecutorTypes)
        {
            var instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                throw new PluginAssemblyLoadException($"无法创建指令处理器 {type.FullName} 的实例");
            }

            var ins = (ICommandExecutor)instance;

            var metadata = ins.GetMetadata();
            if (metadata is null)
            {
                throw new PluginAssemblyLoadException($"无法获取指令处理器 {type.FullName} 的元数据");
            }
            
            _logger.LogTrace("已载入指令处理器 {TraceLoadedCommandHandler}", type.FullName);
            
            manifests.Add(new CommandManifest
            {
                Name = metadata.CommandName,
                Description = metadata.Description,
                HelpText = FormatCommandHelpText(metadata.HelpText),
                PermissionNodesText = FormatCommandPermissionNodesText(metadata.PermissionNodes),
                CommandExecutor = ins
            });
        }

        return manifests;
    }

    /// <summary>
    /// 从 Plugin Assembly 中载入插件生命周期类
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns></returns>
    private static IPluginLifetime? FetchPluginLifetime(IEnumerable<Type> types)
    {
        var type = types.FirstOrDefault(x => x.IsAssignableTo(typeof(IPluginLifetime)));
        if (type is null)
        {
            return null;
        }

        return Activator.CreateInstance(type) as IPluginLifetime;
    }

    /// <summary>
    /// 格式化指令帮助文档输出
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static string FormatCommandHelpText(string message)
    {
        var msg = message.Replace("{{PREFIX}}", HostEnvs.CommandPrefix);

        while (msg.StartsWith("\"") || msg.StartsWith("\n"))
        {
            msg = msg[1..];
        }

        while (msg.EndsWith("\"") || msg.EndsWith("\n"))
        {
            msg = msg[..^1];
        }

        return msg;
    }
    
    /// <summary>
    /// 格式化权限节点文本
    /// </summary>
    /// <returns></returns>
    private static string FormatCommandPermissionNodesText(Dictionary<string, string> permissionNodes)
    {
        if (permissionNodes.Count == 0)
        {
            return "- ***该指令未配置权限节点表帮助文本***";
        }
        
        var sb = new StringBuilder();

        foreach (var (node, explain) in permissionNodes)
        {
            sb.AppendLine($"- `{node}`: {explain}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 读取插件包中的 <c>plugin.json</c>
    /// </summary>
    /// <param name="file">插件包文件</param>
    /// <returns></returns>
    private static async Task<PluginInfo?> ReadPluginInfo(FileInfo file)
    {
        await using var fs = file.OpenRead();
        using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);
        
        var pluginInfoFileEntry = zipArchive.Entries.FirstOrDefault(x => x.Name == "plugin.json");
        if (pluginInfoFileEntry is null)
        {
            return null;
        }
            
        await using var pluginInfoReader = pluginInfoFileEntry.Open();

        var pluginInfo = await JsonSerializer.DeserializeAsync<PluginInfo>(pluginInfoReader);
        return pluginInfo;
    }
}
