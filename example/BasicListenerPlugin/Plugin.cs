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

using DodoHosted.Open.Plugin;

namespace BasicListenerPlugin;

public class Plugin : IPluginLifetime
{
    public Task Load()
    {
        return Task.CompletedTask;
    }

    public Task Unload()
    {
        return Task.CompletedTask;
    }
}
