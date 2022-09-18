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

using MongoDB.Driver;

// ReSharper disable InvertIf
// ReSharper disable SuggestBaseTypeForParameter

namespace DodoHosted.Lib.Plugin.Services;

public class ParameterResolver : IParameterResolver
{
    public object?[] GetCommandInvokeParameter(
        CommandNode node,
        PluginManifest manifest,
        CommandParsed commandParsed,
        PluginBase.Context context)
    {
        var length =
            node.Options.Count +
            node.ServiceOptions.Count +
            (node.ContextParamOrder is null ? 0 : 1);
        var parameters = new object?[length];
        
        var options = node.Options;
        var serviceOptions = node.ServiceOptions;
        
        foreach (var (order, (type, attr)) in options)
        {
            var name = attr.Abbr is null ? new[] { $"-{attr.Name}" } : new[] { $"-{attr.Name}", attr.Abbr };
            var hasValue = commandParsed.Arguments.TryGetValueByMultipleKey(name, out var value);

            if (hasValue is false)
            {
                if (attr.Required)
                {
                    throw new ParameterResolverException($"缺少必填参数 {attr.Name}");
                }
                
                parameters[order] = null;
            }
            else
            {
                var converted = GetOptionParameterValue(type, value!);
                parameters[order] = converted;
            }
        }

        foreach (var (order, type) in serviceOptions)
        {
            var service = GetServiceParameterValue(context.Provider, manifest, type);
            parameters[order] = service;
        }

        return parameters;
    }
    
    public bool ValidateOptionParameterType(Type type)
    {
        var optionType = GetOptionTypeDescriptor(type);
        return optionType is not null;
    }
    
    public bool ValidateServiceParameterType(Type type, bool native = false)
    {
        var serviceType = GetServiceTypeDescriptor(type);

        if (serviceType is null)
        {
            return false;
        }
        
        if (serviceType.NativeOnly)
        {
            if (native is false)
            {
                return false;
            }
        }

        return true;
    }

    public string GetDisplayParameterTypeName(Type type)
    {
        var optionType = GetOptionTypeDescriptor(type);

        if (optionType is null)
        {
            throw new InternalProcessException(
                nameof(ParameterResolver),
                nameof(GetDisplayParameterTypeName),
                $"未知类型 {type.FullName}");
        }

        return optionType.TypeClassName;
    }
    
    private static readonly MethodInfo s_createLoggerMethod = typeof(LoggerFactoryExtensions)
        .GetMethod("CreateLogger", 1, new[] { typeof(ILoggerFactory) })!;
    private static readonly MethodInfo s_getCollectionMethod = typeof(IMongoDatabase)
        .GetMethod("GetCollection", 1, new[] { typeof(string), typeof(MongoCollectionSettings) })!;
    
    private readonly IReadOnlyCollection<CommandOptionTypeDescriptor> _optionTypes =
        new List<CommandOptionTypeDescriptor>
        {
            new(typeof(string), "字符串", x => GetPrimitiveTypeValue(typeof(string), x)),
            new(typeof(int), "整数", x => GetPrimitiveTypeValue(typeof(int), x)),
            new(typeof(long), "长整数", x => GetPrimitiveTypeValue(typeof(long), x)),
            new(typeof(double), "浮点数", x => GetPrimitiveTypeValue(typeof(double), x)),
            new(typeof(bool), "布尔值", x => GetPrimitiveTypeValue(typeof(bool), x)),
            new(typeof(DodoChannelId), "Dodo 频道", x => new DodoChannelId(x).EnsureValid()),
            new(typeof(DodoChannelIdWithWildcard), "Dodo 频道 (包含通配符)", x => new DodoChannelIdWithWildcard(x).EnsureValid()),
            new(typeof(DodoMemberId), "Dodo 用户", x => new DodoMemberId(x).EnsureValid()),
            new(typeof(DodoEmoji), "Emoji", x => new DodoEmoji(x))
        };

    private readonly IReadOnlyCollection<CommandServiceTypeDescriptor> _serviceTypes =
        new List<CommandServiceTypeDescriptor>
        {
            new(typeof(IMongoDatabase), (provider, _, _) => provider.GetRequiredService<IMongoDatabase>()),
            new(typeof(IChannelLogger), (provider, _, _) => provider.GetRequiredService<IChannelLogger>()),
            new(typeof(IPermissionManager), (provider, _, _) => provider.GetRequiredService<IPermissionManager>()),
            new(typeof(OpenApiService), (provider, _, _) => provider.GetRequiredService<OpenApiService>()),
            new(typeof(EventProcessService), (provider, _, _) => provider.GetRequiredService<EventProcessService>()),
            new(typeof(ILogger<>), (provider, _, type) =>
            {
                var factory = provider.GetRequiredService<ILoggerFactory>();
                var genericMethod = s_createLoggerMethod.MakeGenericMethod(type.GetGenericArguments());
                
                return genericMethod.Invoke(null, new object[] { factory })!;
            }),
            new(typeof(IMongoCollection<>), (provider, manifest, type) =>
            {
                var registered = manifest.DodoHostedPlugin.RegisterMongoDbCollection();
                var collectionType = type.GetGenericArguments().FirstOrDefault();

                if (collectionType is null)
                {
                    throw new InternalProcessException(nameof(ParameterResolver), nameof(GetServiceParameterValue), "无法获取泛型参数");
                }

                var contains = registered.ContainsKey(collectionType);
                if (contains is false)
                {
                    throw new ParameterResolverException("未注册的 MongoDb 集合");
                }
                
                var collectionName = registered[collectionType];
                var database = provider.GetRequiredService<IMongoDatabase>();
                
                var genericMethod = s_getCollectionMethod.MakeGenericMethod(collectionType);
                return genericMethod.Invoke(database, new object?[] { collectionName, null })!;
            }),
            new(typeof(PluginConfigurationManager), (provider, manifest, _) =>
            {
                var mongo = provider.GetRequiredService<IMongoDatabase>();
                var configurationVersion = manifest.DodoHostedPlugin.ConfigurationVersion();
                return new PluginConfigurationManager(mongo, manifest.PluginInfo.Identifier, configurationVersion);
            }),
            
            new(typeof(IPluginLifetimeManager), (provider, _, _) => provider.GetRequiredService<IPluginLifetimeManager>(), true),
            new(typeof(IPluginManager), (provider, _, _) => provider.GetRequiredService<IPluginManager>(), true),
            new(typeof(ICommandManager), (provider, _, _) => provider.GetRequiredService<ICommandManager>(), true),
            new(typeof(IEventManager), (provider, _, _) => provider.GetRequiredService<IEventManager>(), true),
            new(typeof(IParameterResolver), (provider, _, _) => provider.GetRequiredService<IParameterResolver>(), true)
        };

    private static object GetPrimitiveTypeValue(Type type, string str)
    {
        try
        {
            var converted = Convert.ChangeType(str, type);
            return converted;
        }
        catch (Exception)
        {
            throw new ParameterResolverException($"无法将 {str} 转换为指定类型 {type.FullName}");
        }
    }

    private object GetOptionParameterValue(Type type, string raw)
    {
        var optionType = GetOptionTypeDescriptor(type);
        if (optionType is null)
        {
            throw new InternalProcessException(
                nameof(ParameterResolver),
                nameof(GetOptionParameterValue),
                $"未知类型 {type.FullName}");
        }

        return optionType.GetValue.Invoke(raw);
    }

    private object GetServiceParameterValue(IServiceProvider provider, PluginManifest manifest, Type type)
    {
        var serviceType = GetServiceTypeDescriptor(type);
        if (serviceType is null)
        {
            throw new InternalProcessException(
                nameof(ParameterResolver),
                nameof(GetServiceParameterValue),
                $"未知类型 {type.FullName}");
        }

        return serviceType.GetValue.Invoke(provider, manifest, type);
    }
    
    private CommandOptionTypeDescriptor? GetOptionTypeDescriptor(Type type)
    {
        // 获取非空类型
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
        
        // 获取类型名称
        var name = nonNullableType.Name.Split('`')[0];
        
        // 获取类型定义
        var optionType = _optionTypes.FirstOrDefault(x => x.TypeClassName == name);

        return optionType;
    }
    
    private CommandServiceTypeDescriptor? GetServiceTypeDescriptor(Type type)
    {
        // 获取类型名称
        var name = type.Name.Split('`')[0];
        
        // 获取类型定义
        var serviceType = _serviceTypes.FirstOrDefault(x => x.TypeClassName == name);

        return serviceType;
    }
}
