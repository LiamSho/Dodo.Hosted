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

using DodoHosted.Lib.Plugin.Models.Module;

namespace DodoHosted.Lib.Plugin.Interfaces;

public interface IPluginManager
{
    bool AddPlugin(PluginModule module);
    bool Exist(string id);
    PluginModule? RemovePlugin(string id);
    IEnumerable<PluginModule> RemovePlugins();
    PluginModule? GetPlugin(string id);
    IEnumerable<PluginModule> GetPlugins();
    IEnumerable<PluginModule> GetPlugins(Func<PluginModule, bool> predicate);
    IEnumerable<PluginModule> GetPlugins(bool native);
    CommandNode? GetCommandNode(string command);
    IEnumerable<CommandNode> GetCommandNodes();
    IEnumerable<CommandNode> GetCommandNodes(string id);
    IEnumerable<CommandNode> GetCommandNodes(Func<CommandNode, bool> predicate);
    CommandExecutorModule? GetCommandExecutorModule(string command);
    IEnumerable<CommandExecutorModule> GetCommandExecutorModules();
    EventHandlerModule? GetEventHandlerModule(string id);
    IEnumerable<EventHandlerModule> GetEventHandlerModules();
    IEnumerable<EventHandlerModule> GetEventHandlerModules(Func<EventHandlerModule, bool> predicate);
}
