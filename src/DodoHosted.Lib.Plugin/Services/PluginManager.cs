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
using System.Xml.Linq;
using DodoHosted.Lib.Plugin.Models.Module;

namespace DodoHosted.Lib.Plugin.Services;

public class PluginManager : IPluginManager
{
    private readonly ConcurrentDictionary<string, PluginModule> _plugins = new();

    public bool AddPlugin(PluginModule manifest)
    {
        return _plugins.TryAdd(manifest.PluginConfigurationModule.PluginInfo.Identifier, manifest);
    }
    public PluginModule? RemovePlugin(string id)
    {
        if (_plugins.ContainsKey(id))
        {
            if (_plugins[id].IsNative)
            {
                return null;
            }
        }
        
        var _ = _plugins.TryRemove(id, out var manifest);
        return manifest;
    }
    public IEnumerable<PluginModule> RemovePlugins()
    {
        var plugins = GetPlugins(x => x.IsNative is false).ToArray();
        foreach (var plugin in plugins)
        {
            _plugins.Remove(plugin.PluginInfo.Identifier, out _);
        }
        return plugins;
    }
    public bool Exist(string id)
    {
        return _plugins.ContainsKey(id);
    }
    
    public PluginModule? GetPlugin(string id)
    {
        return _plugins.TryGetValue(id, out var manifest) ? manifest : null;
    }
    public IEnumerable<PluginModule> GetPlugins()
    {
        return _plugins.Values;
    }
    public IEnumerable<PluginModule> GetPlugins(Func<PluginModule, bool> predicate)
    {
        return _plugins.Values.Where(predicate);
    }
    public IEnumerable<PluginModule> GetPlugins(bool native)
    {
        return GetPlugins(x => x.IsNative == native);
    }

    public CommandNode? GetCommandNode(string command)
    {
        return _plugins.Values
            .Select(x => x.CommandExecutorModule)
            .Select(x => x.GetCommandNode(command))
            .FirstOrDefault(x => x is not null);
    }
    public IEnumerable<CommandNode> GetCommandNodes()
    {
        return _plugins.Values
            .Select(x => x.CommandExecutorModule)
            .SelectMany(x => x.GetCommandNodes());
    }
    public IEnumerable<CommandNode> GetCommandNodes(string id)
    {
        var plugin = GetPlugin(id);
        return plugin is null ? Enumerable.Empty<CommandNode>() : plugin.CommandExecutorModule.GetCommandNodes();
    }
    public IEnumerable<CommandNode> GetCommandNodes(Func<CommandNode, bool> predicate)
    {
        return GetCommandNodes().Where(predicate);
    }

    public CommandExecutorModule? GetCommandExecutorModule(string command)
    {
        return _plugins.Values.FirstOrDefault(x => x.CommandExecutorModule.GetCommandNode(command) is not null)?
            .CommandExecutorModule;
    }
    public IEnumerable<CommandExecutorModule> GetCommandExecutorModules()
    {
        return _plugins.Values.Select(x => x.CommandExecutorModule);
    }

    public EventHandlerModule? GetEventHandlerModule(string id)
    {
        var plugin = GetPlugin(id);
        return plugin?.EventHandlerModule;
    }
    public IEnumerable<EventHandlerModule> GetEventHandlerModules()
    {
        return _plugins.Values.Select(x => x.EventHandlerModule);
    }
    public IEnumerable<EventHandlerModule> GetEventHandlerModules(Func<EventHandlerModule, bool> predicate)
    {
        return GetEventHandlerModules().Where(predicate);
    }
}
