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

using DodoHosted.Base.App.Attributes;
using DodoHosted.Lib.Plugin.Extensions;
using DodoHosted.Lib.Plugin.Models.Module;
using MongoDB.Driver;

namespace DodoHosted.Lib.Plugin.Helper;

public class DynamicDependencyResolver : IDynamicDependencyResolver
{
    private readonly PluginConfigurationModule _pluginConfigurationModule;

    public DynamicDependencyResolver(PluginConfigurationModule pluginConfigurationModule)
    {
        _pluginConfigurationModule = pluginConfigurationModule;
    }

    public static string GetDisplayParameterTypeName(Type type)
    {
        var optionType = GetOptionTypeDescriptor(type);

        if (optionType is null)
        {
            throw new InternalProcessException(
                nameof(DynamicDependencyResolver),
                nameof(GetDisplayParameterTypeName),
                $"未知类型 {type.FullName}");
        }

        return optionType.TypeClassName;
    }

    public T GetDynamicObject<T>(Type type, IServiceProvider serviceProvider)
    {
        var constructorInfo = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(x => x.GetParameters().All(p =>
                p.GetCustomAttribute<InjectAttribute>() is not null));

        if (constructorInfo is null)
        {
            throw new ParameterResolverException($"找不到 {type.FullName} 类型合法的构造函数");
        }

        var parameters = new object?[constructorInfo.GetParameters().Length];
        SetInjectableParameterValues(constructorInfo.GetParameters(), serviceProvider, ref parameters);
        var instance = constructorInfo.Invoke(parameters);

        if (instance is not T t)
        {
            throw new ParameterResolverException($"无法将 {type.FullName} 转换为 {typeof(T).FullName}");
        }

        return t;
    }
    
    #region Parameter Resolver
    
    public void SetCommandOptionParameterValues(CommandNode node, CommandParsed commandParsed, ref object?[] parameters)
    {
        var options = node.Options;
        
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
    }
    public void SetInjectableParameterValues(IEnumerable<ParameterInfo> parameterInfos, IServiceProvider serviceProvider, ref object?[] parameters)
    {
        var injectableParameters = parameterInfos
            .Where(x => x.GetCustomAttribute<InjectAttribute>() is not null);
        
        foreach (var parameterInfo in injectableParameters)
        {
            var p = GetServiceParameterValue(serviceProvider, parameterInfo.ParameterType);
            parameters[parameterInfo.Position] = p;
        }
    }

    #endregion

    #region Base Static Members

    private static CommandOptionTypeDescriptor? GetOptionTypeDescriptor(Type type)
    {
        var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
        var name = nonNullableType.Name.Split('`')[0];
        var optionType = s_optionTypes.FirstOrDefault(x => x.TypeClassName == name);

        return optionType;
    }
    private static CommandServiceTypeDescriptor? GetServiceTypeDescriptor(MemberInfo memberInfo)
    {
        var name = memberInfo.Name.Split('`')[0];
        var serviceType = s_serviceTypes.FirstOrDefault(x => x.TypeClassName == name);

        return serviceType;
    }
    
    private static readonly MethodInfo s_createLoggerMethod = typeof(LoggerFactoryExtensions)
        .GetMethod("CreateLogger", 1, new[] { typeof(ILoggerFactory) })!;
    private static readonly MethodInfo s_getCollectionMethod = typeof(IMongoDatabase)
        .GetMethod("GetCollection", 1, new[] { typeof(string), typeof(MongoCollectionSettings) })!;
        
    private static readonly IReadOnlyCollection<CommandOptionTypeDescriptor> s_optionTypes =
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

    private static readonly IReadOnlyCollection<CommandServiceTypeDescriptor> s_serviceTypes =
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
            new(typeof(IMongoCollection<>), (provider, plugin, type) =>
            {
                var registered = plugin.Instance.RegisterMongoDbCollection();
                var collectionType = type.GetGenericArguments().FirstOrDefault();

                if (collectionType is null)
                {
                    throw new InternalProcessException(nameof(DynamicDependencyResolver), nameof(s_serviceTypes), "无法获取泛型参数");
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
            new(typeof(PluginConfigurationManager), (provider, plugin, _) =>
            {
                var mongo = provider.GetRequiredService<IMongoDatabase>();
                var configurationVersion = plugin.Instance.ConfigurationVersion();
                return new PluginConfigurationManager(mongo, plugin.PluginInfo.Identifier, configurationVersion);
            }),
            
            new(typeof(IPluginLoadingManager), (provider, _, _) => provider.GetRequiredService<IPluginLoadingManager>(), true),
            new(typeof(IPluginManager), (provider, _, _) => provider.GetRequiredService<IPluginManager>(), true),
            new(typeof(ICommandManager), (provider, _, _) => provider.GetRequiredService<ICommandManager>(), true),
            new(typeof(IEventManager), (provider, _, _) => provider.GetRequiredService<IEventManager>(), true)
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
    
    #endregion
    
    private object GetOptionParameterValue(Type type, string raw)
    {
        var optionType = GetOptionTypeDescriptor(type);
        if (optionType is null)
        {
            throw new InternalProcessException(
                nameof(DynamicDependencyResolver),
                nameof(GetOptionParameterValue),
                $"未知类型 {type.FullName}");
        }

        return optionType.GetValue.Invoke(raw);
    }
    private object GetServiceParameterValue(IServiceProvider provider, Type type)
    {
        var serviceType = GetServiceTypeDescriptor(type);
        if (serviceType is null)
        {
            throw new InternalProcessException(
                nameof(DynamicDependencyResolver),
                nameof(GetServiceParameterValue),
                $"未知类型 {type.FullName}");
        }

        return serviceType.GetValue.Invoke(provider, _pluginConfigurationModule, type);
    }
}
