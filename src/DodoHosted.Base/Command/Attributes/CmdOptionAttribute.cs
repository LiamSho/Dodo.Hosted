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

namespace DodoHosted.Base.Command.Attributes;

/// <summary>
/// 指令参数 Attribute
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
public class CmdOptionAttribute : Attribute
{
    public string Name { get;}
    public string? Abbr { get; }
    public bool Required { get; }
    public string Description { get; }

    /// <summary>
    /// 指令参数 Attribute
    /// </summary>
    /// <param name="name">参数名称</param>
    /// <param name="abbr">参数简写</param>
    /// <param name="description">简介，默认为空</param>
    /// <param name="required">是否为必须，默认为 true</param>
    public CmdOptionAttribute(string name, string? abbr = null, string description = "", bool required = true)
    {
        Name = name;
        Abbr = abbr;
        Required = required;
        Description = description;
    }
}
