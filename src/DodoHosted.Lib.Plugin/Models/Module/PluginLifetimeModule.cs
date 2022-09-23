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

public class PluginLifetimeModule : IDisposable
{
    private readonly IServiceScope _serviceScope;
    
    public DodoHostedPluginLifetime PluginLifetime { get; }
    
    public PluginLifetimeModule(
        IEnumerable<Type> types,
        IServiceScope serviceScope,
        IDynamicDependencyResolver dependencyResolver)
    {
        _serviceScope = serviceScope;

        var type = types
                       .Where(x => x.IsSealed)
                       .Where(x => x != typeof(DodoHostedPluginLifetime))
                       .FirstOrDefault(x => x.IsAssignableTo(typeof(DodoHostedPluginLifetime)))
                   ?? typeof(DefaultPluginLifetime);
        
        var instance = dependencyResolver.GetDynamicObject<DodoHostedPluginLifetime>(type, _serviceScope.ServiceProvider);

        PluginLifetime = instance;
    }

    public void Dispose()
    {
        _serviceScope.Dispose();
        
        GC.SuppressFinalize(this);
    }
}
