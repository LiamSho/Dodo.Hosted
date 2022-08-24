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

namespace DodoHosted.Base.Attributes;

/// <summary>
/// 标记枚举项为列表选择器的选项
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ListSelectorAttribute : Attribute
{
    public ListSelectorAttribute(string name, string description)
    {
        Description = description;
        Name = name;
    }

    public string Name { get; }
    public string Description { get; }
}
