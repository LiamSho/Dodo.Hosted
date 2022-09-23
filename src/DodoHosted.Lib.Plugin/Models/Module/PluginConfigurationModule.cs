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

public class PluginConfigurationModule
{
    public PluginInfo PluginInfo { get; }
    public DodoHostedPluginConfiguration Instance { get; }
    
    public PluginConfigurationModule(IEnumerable<Type> types, PluginInfo pluginInfo)
    {
        PluginInfo = pluginInfo;
        
        var type = types
                       .Where(x => x.IsSealed)
                       .Where(x => x != typeof(DefaultPluginConfiguration))
                       .FirstOrDefault(x => x.IsAssignableTo(typeof(DodoHostedPluginConfiguration)))
                   ?? typeof(DefaultPluginConfiguration);

        var instance = Activator.CreateInstance(type);
        
        if (instance is not DodoHostedPluginConfiguration plugin)
        {
            throw new PluginAssemblyLoadException($"无法实例化 {type.FullName}");
        }

        Instance = plugin;
    }
}
