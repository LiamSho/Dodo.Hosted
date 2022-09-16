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

namespace DodoHosted.Lib.Plugin.Models.Manifest;

public class PluginWorker
{
    /// <summary>
    /// 插件所含 Event Handler
    /// </summary>
    public required EventHandlerManifest[] EventHandlers { get; init; }
    
    /// <summary>
    /// 插件所含指令
    /// </summary>
    public required CommandManifest[] CommandExecutors { get; init; }
    
    /// <summary>
    /// 插件所含后台服务
    /// </summary>
    public required HostedServiceManifest[] HostedServices { get; init; }
}
