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

namespace DodoHosted.Lib.Plugin.Models.Module;

public class EventHandlerModule : IDisposable
{
    private readonly List<KeyValuePair<string, Func<IDodoHostedEvent, Task>>> _invokers;

    public EventHandlerModule(IEnumerable<Type> types, IDynamicDependencyResolver dependencyResolver, IServiceProvider serviceProvider)
    {
        _invokers = new List<KeyValuePair<string, Func<IDodoHostedEvent, Task>>>();

        var eventHandlerTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(IEventHandler<>))
            .Where(x => x
                .GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IEventHandler<>)))
            .Where(x => x.ContainsGenericParameters is false);

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
            
            _invokers.Add(new KeyValuePair<string, Func<IDodoHostedEvent, Task>>(
                eventType.FullName!,
                async e =>
                {
                    var scope = serviceProvider.CreateScope();

                    var parameterInfos = constructor.GetParameters();
                    var parameters = new object?[parameterInfos.Length];
                    
                    dependencyResolver.SetInjectableParameterValues(parameterInfos, scope.ServiceProvider, ref parameters);
                    var ins = constructor.Invoke(parameters);
                    
                    await (Task)method.Invoke(ins, new object?[] {e})!;
                    
                    scope.Dispose();
                }));
        }
    }

    public IEnumerable<Func<IDodoHostedEvent, Task>> GetEventHandlers(string typeString)
    {
        return _invokers.Where(x => x.Key == typeString).Select(x => x.Value);
    }
    
    public async Task<int> Invoke(string typeString, IDodoHostedEvent e)
    {
        var error = new List<KeyValuePair<string, Exception>>();
        var count = 0;
        
        foreach (var (t, invoker) in _invokers)
        {
            try
            {
                if (t != typeString)
                {
                    continue;
                }

                await invoker.Invoke(e);
                count++;
            }
            catch (Exception ex)
            {
                error.Add(new KeyValuePair<string, Exception>(typeString, ex));
            }
        }

        if (error.Count != 0)
        {
            throw new EventHandlerExecutionException(error);
        }

        return count;
    }

    public int Count()
    {
        return _invokers.Count;
    }
    
    public void Dispose()
    {
        _invokers.Clear();
    
        GC.SuppressFinalize(this);
    }
}
