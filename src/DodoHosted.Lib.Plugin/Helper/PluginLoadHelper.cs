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
using DodoHosted.Base.App.Attributes;

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

    /// <summary>
    /// 加载插件工作类型
    /// </summary>
    /// <param name="pluginTypes"></param>
    /// <param name="provider"></param>
    /// <param name="id"></param>
    /// <param name="native"></param>
    /// <returns></returns>
    internal static PluginWorker LoadPluginWorkers(this IEnumerable<Type> pluginTypes, IServiceProvider provider, string id, bool native = false)
    {
        var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("PluginWorkerLoader");
        var parameterResolver = provider.GetRequiredService<IParameterResolver>();
        
        var types = pluginTypes.ToArray();

        var eventHandlers = types.FetchEventHandlers(id, logger);
        var webHandlers = types.FetchWebHandlers(id, logger);
        var commandExecutors = types.FetchCommandExecutors(id, logger);
        var hostedServices = types.FetchHostedService(id, logger, provider);
        
        foreach (var executor in commandExecutors)
        {
            var maxDepth = executor.RootNode.MaxDepth;
            for (var i = 1; i <= maxDepth; i++)
            {
                var nodes = executor.RootNode
                    .GetNodes(i)
                    .Where(x => x.Method is not null);

                foreach (var node in nodes)
                {
                    foreach (var (_, (type, attr)) in node.Options)
                    {
                        var result = parameterResolver.ValidateOptionParameterType(type);
                        if (result is false)
                        {
                            throw new PluginAssemblyLoadException($"指令参数 {attr.Name} 的类型 {type.FullName} 不支持");
                        }
                    }

                    foreach (var (_, type) in node.ServiceOptions)
                    {
                        var result = parameterResolver.ValidateServiceParameterType(type, native);
                        if (result is false)
                        {
                            throw new PluginAssemblyLoadException($"指令服务参数 {type.FullName} 不支持");
                        }
                    }
                }
            }
        }

        foreach (var eventHandler in eventHandlers)
        {
            var constructorParameterTypes = eventHandler.EventHandlerConstructor
                .GetParameters()
                .Select(x => x.ParameterType);

            foreach (var type in constructorParameterTypes)
            {
                var result = parameterResolver.ValidateServiceParameterType(type, native);
                if (result is false)
                {
                    throw new PluginAssemblyLoadException($"事件执行器服务参数 {type.FullName} 不支持");
                }
            }
        }

        foreach (var webHandler in webHandlers)
        {
            var constructorParameterTypes = webHandler.WebHandlerConstructor
                .GetParameters()
                .Select(x => x.ParameterType);

            foreach (var type in constructorParameterTypes)
            {
                var result = parameterResolver.ValidateServiceParameterType(type, native);
                if (result is false)
                {
                    throw new PluginAssemblyLoadException($"Web 执行器服务参数 {type.FullName} 不支持");
                }
            }
        }
        
        foreach (var service in hostedServices)
        {
            var serviceLogger = service.Scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(service.Name);
            service.JobTask = service.Job.StartAsync(service.Scope.ServiceProvider, serviceLogger, service.CancellationTokenSource.Token);
        }
        
        return new PluginWorker
        {
            CommandExecutors = commandExecutors,
            EventHandlers = eventHandlers,
            HostedServices = hostedServices,
            WebHandlers = webHandlers
        };
    }

    /// <summary>
    /// 卸载插件
    /// </summary>
    /// <param name="manifest"></param>
    /// <param name="logger"></param>
    /// <param name="loadFailed"></param>
    internal static void UnloadPlugin(this PluginManifest manifest, ILogger logger, bool loadFailed = false)
    {
        manifest.Worker.HostedServices.AsParallel()
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
                        logger.LogError(ex, "取消 Task 出现异常");
                    }
                }
                x.Scope.Dispose();
                logger.LogInformation("停止插件后台任务 {PluginHostedServiceName}", x.Name);
            });

        if (loadFailed is false)
        {
            manifest.DodoHostedPlugin.OnDestroy().GetAwaiter().GetResult();
        }
        
        manifest.PluginScope.Dispose();
        
        if (manifest.IsNative)
        {
            return;
        }

        manifest.Context!.Unload();

        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>
    /// 载入所有的事件处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <param name="id">Plugin Identifier</param>
    /// <param name="logger"></param>
    /// <returns><see cref="EventHandlerManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private static EventHandlerManifest[] FetchEventHandlers(this IEnumerable<Type> types, string id, ILogger logger)
    {
        var eventHandlerTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(IEventHandler<>))
            .Where(x => x
                .GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
            .Where(x => x.ContainsGenericParameters is false);

        var manifests = new List<EventHandlerManifest>();
        foreach (var type in eventHandlerTypes)
        {
            var interfaceType = type.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventHandler<>));

            var constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(x => x.GetParameters().All(y =>
                    y.GetCustomAttribute<InjectAttribute>() is not null));
            
            var method = interfaceType.GetMethod("Handle");
            var eventType = interfaceType.GetGenericArguments().FirstOrDefault();

            if (constructor is null)
            {
                throw new PluginAssemblyLoadException($"找不到事件处理器 {type.FullName} 的合法构造函数");
            }
            if (method is null)
            {
                throw new PluginAssemblyLoadException($"找不到事件处理器 {type.FullName} 的 Handle 方法");
            }
            if (eventType is null)
            {
                throw new PluginAssemblyLoadException($"找不到事件处理器 {type.FullName} 的事件类型");
            }
            
            logger.LogInformation("已载入事件处理器 {LoadedEventHandler}", type.FullName);
            
            manifests.Add(new EventHandlerManifest
            {
                EventHandlerConstructor = constructor,
                EventType = eventType,
                EventTypeString = eventType.FullName!,
                EventHandlerType = type,
                HandlerMethod = method,
                PluginIdentifier = id
            });
        }

        return manifests.ToArray();
    }

    private static WebHandlerManifest[] FetchWebHandlers(this IEnumerable<Type> types, string id, ILogger logger)
    {
        var webHandlerTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(IPluginWebHandler))
            .Where(x => x.IsAssignableTo(typeof(IPluginWebHandler)));

        var manifests = new List<WebHandlerManifest>();
        foreach (var webHandlerType in webHandlerTypes)
        {
            var attr = webHandlerType.GetCustomAttribute<NameAttribute>();
            var name = attr?.Name ?? webHandlerType.Name;

            if (attr is null)
            {
                logger.LogWarning("{WebHandlerTypeName} 缺少 NameAttribute，默认使用类型名作为名称", webHandlerType.FullName);
            }

            var interfaceType = webHandlerType.GetInterfaces().First(x => x == typeof(IPluginWebHandler));

            var constructor = webHandlerType.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(x => x.GetParameters().All(y =>
                    y.GetCustomAttribute<InjectAttribute>() is not null));
            
            var method = interfaceType.GetMethod("Handle");
            
            if (constructor is null)
            {
                throw new PluginAssemblyLoadException($"找不到 Web 事件处理器 {webHandlerType.FullName} 的合法构造函数");
            }
            if (method is null)
            {
                throw new PluginAssemblyLoadException($"找不到 Web 事件处理器 {webHandlerType.FullName} 的 Handle 方法");
            }
            
            logger.LogInformation("已载入 Web 事件处理器 {LoadedWebHandlerName} -> {LoadedWebHandlerType}", name, webHandlerType.FullName);
            
            manifests.Add(new WebHandlerManifest
            {
                HandlerMethod = method,
                Name = name,
                PluginIdentifier = id,
                WebHandlerConstructor = constructor,
                WebHandlerType = webHandlerType
            });
        }

        return manifests.ToArray();
    }

    /// <summary>
    /// 载入所有的指令处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <param name="id">Plugin Identifier</param>
    /// <param name="logger"></param>
    /// <returns><see cref="CommandManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private static CommandManifest[] FetchCommandExecutors(this IEnumerable<Type> types, string id, ILogger logger)
    {
        var commandExecutorTypes = types
            .Where(x => x.IsSealed)
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

            var root = ins.GetBuilder().Build();
            
            logger.LogInformation("已载入指令处理器 {LoadedCommandHandler}", type.FullName);
            
            manifests.Add(new CommandManifest
            {
                CommandExecutor = ins,
                RootNode = root,
                PluginIdentifier = id
            });
        }

        return manifests.ToArray();
    }

    /// <summary>
    /// 载入所有的后台服务
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <param name="id">Plugin Identifier</param>
    /// <param name="logger"></param>
    /// <param name="provider"></param>
    /// <returns><see cref="HostedServiceManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private static HostedServiceManifest[] FetchHostedService(this IEnumerable<Type> types, string id, ILogger logger, IServiceProvider provider)
    {
        var hostedServiceTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(IPluginHostedService))
            .Where(x => x.IsAssignableTo(typeof(IPluginHostedService)))
            .ToList();

        var manifests = new List<HostedServiceManifest>();
        foreach (var type in hostedServiceTypes)
        {
            var instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                throw new PluginAssemblyLoadException($"无法创建后台服务 {type.FullName} 的实例");
            }
            
            var ins = (IPluginHostedService)instance;
            
            logger.LogInformation("已载入后台服务 {LoadedHostedService} ({LoadedHostedServiceName})", type.FullName, ins.HostedServiceName);
            
            manifests.Add(new HostedServiceManifest
            {
                Job = ins,
                Scope = provider.CreateScope(),
                Name = ins.HostedServiceName,
                PluginIdentifier = id
            });
        }

        return manifests.ToArray();
    }

    /// <summary>
    /// 载入插件实例类
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns></returns>
    internal static DodoHostedPlugin? FetchPluginInstance(this IEnumerable<Type> types)
    {
        var type = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(DefaultPluginInstance))
            .FirstOrDefault(x => x.IsAssignableTo(typeof(DodoHostedPlugin)));
        if (type is null)
        {
            return null;
        }

        var ins = Activator.CreateInstance(type);
        
        if (ins is not DodoHostedPlugin plugin)
        {
            throw new PluginAssemblyLoadException($"无法实例化 {type.FullName}");
        }

        return plugin;
    }
}
