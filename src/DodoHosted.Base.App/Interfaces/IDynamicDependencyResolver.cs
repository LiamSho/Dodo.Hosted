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
using DodoHosted.Base.App.Command;
using DodoHosted.Base.App.Models;

namespace DodoHosted.Base.App.Interfaces;

public interface IDynamicDependencyResolver
{
    T GetDynamicObject<T>(Type type, IServiceProvider serviceProvider);
    void SetCommandOptionParameterValues(CommandNode node, CommandParsed commandParsed, ref object?[] parameters);
    void SetInjectableParameterValues(IEnumerable<ParameterInfo> parameterInfos, IServiceProvider serviceProvider, ref object?[] parameters);
}
