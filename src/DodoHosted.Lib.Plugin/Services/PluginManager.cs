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
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Lib.Plugin.Interfaces;
using DodoHosted.Lib.Plugin.Models.Manifest;

namespace DodoHosted.Lib.Plugin.Services;

public class PluginManager : IPluginManager
{
    private readonly ConcurrentDictionary<string, PluginManifest> _plugins = new();

    public bool AddPlugin(PluginManifest manifest)
    {
        return _plugins.TryAdd(manifest.PluginInfo.Identifier, manifest);
    }
    public PluginManifest? RemovePlugin(string id)
    {
        var _ = _plugins.TryRemove(id, out var manifest);
        return manifest;
    }

    public IEnumerable<PluginManifest> RemovePlugins()
    {
        var plugins = GetPlugins().ToArray();
        _plugins.Clear();
        return plugins;
    }
    public bool Exist(string id)
    {
        return _plugins.ContainsKey(id);
    }
    
    public PluginManifest? GetPlugin(string id)
    {
        return _plugins.TryGetValue(id, out var manifest) ? manifest : null;
    }
    public IEnumerable<PluginManifest> GetPlugins()
    {
        return _plugins.Values;
    }
    public IEnumerable<PluginManifest> GetPlugins(Func<PluginManifest, bool> predicate)
    {
        return _plugins.Values.Where(predicate);
    }

    public IEnumerable<PluginManifest> GetPlugins(bool native)
    {
        return GetPlugins(x => x.IsNative == native);
    }

    public CommandManifest? GetCommandManifest(string command)
    {
        return GetCommandManifests(x => x.RootNode.Value == command).FirstOrDefault();
    }
    public IEnumerable<CommandManifest> GetCommandManifests()
    {
        return _plugins.Values.Select(x => x.Worker).SelectMany(x => x.CommandExecutors);
    }
    public IEnumerable<CommandManifest> GetCommandManifests(string id)
    {
        var manifest = GetPlugin(id);
        return manifest?.Worker.CommandExecutors ?? Enumerable.Empty<CommandManifest>();
    }
    public IEnumerable<CommandManifest> GetCommandManifests(Func<CommandManifest, bool> predicate)
    {
        return GetCommandManifests().Where(predicate);
    }
    public IEnumerable<EventHandlerManifest> GetEventHandlerManifests()
    {
        return _plugins.Values.Select(x => x.Worker).SelectMany(x => x.EventHandlers);
    }
    public IEnumerable<EventHandlerManifest> GetEventHandlerManifests(string id)
    {
        var manifest = GetPlugin(id);
        return manifest?.Worker.EventHandlers ?? Enumerable.Empty<EventHandlerManifest>();
    }
    public IEnumerable<EventHandlerManifest> GetEventHandlerManifests(Func<EventHandlerManifest, bool> predicate)
    {
        return GetEventHandlerManifests().Where(predicate);
    }
}
