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

using Microsoft.Extensions.Hosting;

namespace DodoHosted.Lib.Plugin;

public class PluginSystemHosted : IHostedService
{
    private readonly IPluginManager _pluginManager;

    public PluginSystemHosted(IPluginManager pluginManager)
    {
        _pluginManager = pluginManager;
    }
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await _pluginManager.LoadPlugins();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _pluginManager.UnloadPlugins();
        return Task.CompletedTask;
    }
}
