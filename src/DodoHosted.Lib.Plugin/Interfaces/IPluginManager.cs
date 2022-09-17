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

namespace DodoHosted.Lib.Plugin.Interfaces;

public interface IPluginManager
{
    bool AddPlugin(PluginManifest manifest);
    bool Exist(string id);
    PluginManifest? RemovePlugin(string id);
    IEnumerable<PluginManifest> RemovePlugins();
    PluginManifest? GetPlugin(string id);
    IEnumerable<PluginManifest> GetPlugins();
    IEnumerable<PluginManifest> GetPlugins(Func<PluginManifest, bool> predicate);
    IEnumerable<PluginManifest> GetPlugins(bool native);
    CommandManifest? GetCommandManifest(string command);
    IEnumerable<CommandManifest> GetCommandManifests();
    IEnumerable<CommandManifest> GetCommandManifests(string id);
    IEnumerable<CommandManifest> GetCommandManifests(Func<CommandManifest, bool> predicate);
    IEnumerable<EventHandlerManifest> GetEventHandlerManifests();
    IEnumerable<EventHandlerManifest> GetEventHandlerManifests(string id);
    IEnumerable<EventHandlerManifest> GetEventHandlerManifests(Func<EventHandlerManifest, bool> predicate);
}
