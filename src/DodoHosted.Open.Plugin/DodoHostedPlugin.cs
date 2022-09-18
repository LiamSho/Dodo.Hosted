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

namespace DodoHosted.Open.Plugin;

public abstract class DodoHostedPlugin
{
    public abstract Task OnLoad();
    public abstract Task OnDestroy();

    public abstract int ConfigurationVersion();

    public virtual Dictionary<Type, string> RegisterMongoDbCollection()
    {
        return new Dictionary<Type, string>();
    }
}
