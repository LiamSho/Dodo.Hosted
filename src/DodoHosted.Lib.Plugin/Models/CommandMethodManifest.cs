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

using System.Reflection;
using DodoHosted.Open.Plugin.Attributes;

namespace DodoHosted.Lib.Plugin.Models;

public record CommandMethodManifest
{
    public required MethodInfo Method { get; init; }
    public required string[] Path { get; init; }
    public required string PermissionNode { get; init; }
    public required int ContextParamOrder { get; init; }
    public required string Description { get; init; }
    public required Dictionary<int, (Type, CmdOptionAttribute)> Options { get; init; }
}
