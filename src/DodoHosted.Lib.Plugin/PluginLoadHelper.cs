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
using DodoHosted.Lib.Plugin.Exceptions;
using DodoHosted.Lib.Plugin.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.Plugin;

internal static class PluginLoadHelper
{
    /// <summary>
    /// 载入所有的事件处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <param name="logger"></param>
    /// <returns><see cref="EventHandlerManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    internal static IEnumerable<EventHandlerManifest> FetchEventHandlers(this IEnumerable<Type> types, ILogger logger)
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
                throw new PluginAssemblyLoadException($"无法创建事件处理器 {type.FullName} 的实例");
            }
            if (method is null)
            {
                throw new PluginAssemblyLoadException($"找不到到事件处理器 {type.FullName} 的 Handle 方法");
            }
            if (eventType is null)
            {
                throw new PluginAssemblyLoadException($"找不到到事件处理器 {type.FullName} 的事件类型");
            }
            
            logger.LogInformation("已载入事件处理器 {LoadedEventHandler}", type.FullName);
            
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
    /// 载入所有的指令处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <param name="logger"></param>
    /// <param name="serviceProvider"></param>
    /// <returns><see cref="CommandManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    internal static IEnumerable<CommandManifest> FetchCommandExecutors(this IEnumerable<Type> types, ILogger logger, IServiceProvider serviceProvider)
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

            var root = ins.GetBuilder().Build(serviceProvider);
            
            logger.LogInformation("已载入指令处理器 {LoadedCommandHandler}", type.FullName);
            
            manifests.Add(new CommandManifest
            {
                CommandExecutor = ins,
                RootNode = root
            });
        }

        return manifests;
    }
    
    /// <summary>
    /// 载入所有的后台服务
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <param name="logger"></param>
    /// <param name="provider"></param>
    /// <returns><see cref="HostedServiceManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    internal static IEnumerable<HostedServiceManifest> FetchHostedService(this IEnumerable<Type> types, ILogger logger, IServiceProvider provider)
    {
        var hostedServiceTypes = types
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
                Name = ins.HostedServiceName
            });
        }

        return manifests;
    }

    /// <summary>
    /// 载入插件生命周期类
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns></returns>
    internal static IPluginLifetime? FetchPluginLifetime(this IEnumerable<Type> types)
    {
        var type = types.FirstOrDefault(x => x.IsAssignableTo(typeof(IPluginLifetime)));
        if (type is null)
        {
            return null;
        }

        return Activator.CreateInstance(type) as IPluginLifetime;
    }
    
    /// <summary>
    /// 读取插件包中的 <c>plugin.json</c>
    /// </summary>
    /// <param name="file">插件包文件</param>
    /// <returns></returns>
    internal static async Task<PluginInfo?> ReadPluginInfo(FileInfo file)
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
