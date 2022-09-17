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

public interface ICommandParameterResolver
{
    object?[] GetMethodInvokeParameter(CommandNode node, PluginManifest manifest, CommandParsed commandParsed, PluginBase.Context context);
    bool ValidateOptionParameterType(Type type);
    bool ValidateServiceParameterType(Type type, bool native = false);
    string GetDisplayParameterTypeName(Type type);
}
