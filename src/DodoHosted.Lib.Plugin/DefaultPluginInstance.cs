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

namespace DodoHosted.Lib.Plugin;

public class DefaultPluginInstance : DodoHostedPlugin
{
    public override Task OnLoad()
    {
        return Task.CompletedTask;
    }

    public override Task OnDestroy()
    {
        return Task.CompletedTask;
    }

    public override Dictionary<Type, string> RegisterMongoDbCollection()
    {
        return new Dictionary<Type, string>();
    }
}
