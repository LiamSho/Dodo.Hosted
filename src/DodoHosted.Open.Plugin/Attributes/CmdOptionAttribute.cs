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

namespace DodoHosted.Open.Plugin.Attributes;

[AttributeUsage(AttributeTargets.Parameter)]
public class CmdOptionAttribute : Attribute
{
    public string Name { get;}
    public string? Abbr { get; }
    public bool Required { get; }
    public string HelpText { get; }
    public object? Default { get; }

    public CmdOptionAttribute(string name, string? abbr = null, bool required = true, string helpText = "", object? defaultValue = default)
    {
        Name = name;
        Abbr = abbr;
        Required = required;
        HelpText = helpText;
        Default = defaultValue;
    }
}
