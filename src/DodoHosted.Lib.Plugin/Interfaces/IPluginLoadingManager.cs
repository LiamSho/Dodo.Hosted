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

public interface IPluginLoadingManager
{
    Task LoadPlugin(FileInfo bundle);
    Task LoadPlugin(string bundle);
    Task LoadPlugins();
    bool UnloadPlugin(string pluginIdentifier);
    void UnloadPlugins();
    void LoadNativeTypes();
    void UnloadNativeTypes();
    Task<(Dictionary<string, PluginInfo>, Dictionary<string, Exception>)> GetUnloadedPlugins();
}
