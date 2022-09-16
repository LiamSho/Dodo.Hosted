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

using DodoHosted.Base;
using DodoHosted.Base.App.Command;
using DodoHosted.Base.App.Models;
using DodoHosted.Lib.Plugin.Models.Manifest;

namespace DodoHosted.Lib.Plugin.Interfaces;

public interface ICommandParameterHelper
{
    object?[] GetMethodInvokeParameter(CommandNode node, PluginManifest manifest, CommandParsed commandParsed, PluginBase.Context context);
    bool ValidateOptionType(Type type);
    bool ValidateServiceType(Type type, bool native = false);
    string GetDisplayTypeName(Type type);
}
