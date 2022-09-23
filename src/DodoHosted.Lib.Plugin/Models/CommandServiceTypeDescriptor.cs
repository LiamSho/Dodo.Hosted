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

namespace DodoHosted.Lib.Plugin.Models;

public record CommandServiceTypeDescriptor(
    Type Type,
    Func<IServiceProvider, PluginConfigurationModule, Type, object> GetValue,
    bool NativeOnly = false)
{
    public string TypeClassName => Type.Name.Split('`').First();
}
