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

namespace DodoHosted.Base.App.Exceptions;

public class PluginConfigurationException : Exception
{
    public PluginConfigurationException(string pluginId, string key)
        : base($"找不到插件 {pluginId} 配置 {key}") { }

    public PluginConfigurationException(string pluginId, string key, Type t)
        : base($"插件 {pluginId} 配置 {key} 不符合类型 {t.FullName}") { }
    
    public PluginConfigurationException(string invalidKey)
        : base($"无效的配置键 {invalidKey}") { }
}
