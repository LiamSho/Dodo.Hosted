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
/// 设置长度限制
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class FormLimitAttribute : Attribute
{
    public FormLimitAttribute(int minCharacters, int maxCharacters, int rows = 1)
    {
        MaxCharacters = maxCharacters;
        MinCharacters = minCharacters;
        Rows = rows;
    }

    public int MinCharacters { get; }
    public int MaxCharacters { get; }
    public int Rows { get; }
}
