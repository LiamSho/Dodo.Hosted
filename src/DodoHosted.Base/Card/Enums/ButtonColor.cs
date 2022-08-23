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

using DodoHosted.Base.JsonExtension;

namespace DodoHosted.Base.Card.Enums;

/// <summary>
/// 按钮颜色
/// </summary>
[StringValueTypeWriteConvertor<ButtonColor>]
public class ButtonColor : StringValueType, IStringValueType<ButtonColor>
{
    private ButtonColor(string value) : base(value) { }
    
    /// <summary>
    /// 默认
    /// </summary>
    public static ButtonColor Default => new("default");
    
    /// <summary>
    /// 灰色
    /// </summary>
    public static ButtonColor Grey => new("grey");
    
    /// <summary>
    /// 红色
    /// </summary>
    public static ButtonColor Red => new("red");
    
    /// <summary>
    /// 橙色
    /// </summary>
    public static ButtonColor Orange => new("orange");
    
    /// <summary>
    /// 绿色
    /// </summary>
    public static ButtonColor Green => new("green");
    
    /// <summary>
    /// 蓝色
    /// </summary>
    public static ButtonColor Blue => new("blue");
    
    /// <summary>
    /// 紫色
    /// </summary>
    public static ButtonColor Purple => new("purple");
    
    public static IEnumerable<ButtonColor> SupportedValues
    {
        get
        {
            yield return Default;
            yield return Grey;
            yield return Red;
            yield return Orange;
            yield return Green;
            yield return Blue;
            yield return Purple;
        }
    }
}
