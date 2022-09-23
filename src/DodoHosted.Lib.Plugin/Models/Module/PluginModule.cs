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

namespace DodoHosted.Lib.Plugin.Models.Module;

public class PluginModule
{
    private IDynamicDependencyResolver DependencyResolver { get; }
    private PluginLifetimeModule PluginLifetimeModule { get; }
    private PluginAssemblyLoadContext? AssemblyLoadContext { get; }

    public PluginConfigurationModule PluginConfigurationModule { get; }
    public CommandExecutorModule CommandExecutorModule { get; }
    public HostedServiceModule HostedServiceModule { get; }
    public EventHandlerModule EventHandlerModule { get; }
    public WebHandlerModule WebHandlerModule { get; }
    
    public PluginInfo PluginInfo { get; }
    public bool IsNative { get; }
    
    private readonly IServiceScope _pluginScope;
    
    public PluginModule(
        PluginAssemblyLoadContext? assemblyLoadContext,
        IEnumerable<Assembly> assemblies,
        PluginInfo pluginInfo,
        IServiceProvider serviceProvider,
        bool isNative = false)
    {
        AssemblyLoadContext = assemblyLoadContext;
        PluginInfo = pluginInfo;
        
        var types = assemblies.SelectMany(x => x.GetTypes()).ToArray();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        
        IsNative = isNative;
        
        _pluginScope = serviceProvider.CreateScope();
        
        PluginConfigurationModule = new PluginConfigurationModule(types, pluginInfo);

        DependencyResolver = new DynamicDependencyResolver(PluginConfigurationModule);

        PluginLifetimeModule = new PluginLifetimeModule(types, _pluginScope.ServiceProvider.CreateScope(), DependencyResolver);
        CommandExecutorModule = new CommandExecutorModule(types, DependencyResolver);
        HostedServiceModule = new HostedServiceModule(types, _pluginScope.ServiceProvider, DependencyResolver, loggerFactory.CreateLogger<HostedServiceModule>());
        EventHandlerModule = new EventHandlerModule(types, DependencyResolver, serviceProvider);
        WebHandlerModule = new WebHandlerModule(types, serviceProvider, DependencyResolver, loggerFactory.CreateLogger<WebHandlerModule>());

        PluginLifetimeModule.PluginLifetime.OnLoad().GetAwaiter().GetResult();
    }

    public void Unload()
    {
        PluginLifetimeModule.PluginLifetime.OnDestroy().GetAwaiter().GetResult();
        
        PluginLifetimeModule.Dispose();
        
        CommandExecutorModule.Dispose();
        HostedServiceModule.Dispose();
        EventHandlerModule.Dispose();
        WebHandlerModule.Dispose();
        
        _pluginScope.Dispose();
        
        AssemblyLoadContext?.Unload();
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}
