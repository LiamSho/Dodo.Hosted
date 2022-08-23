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
/// 卡片主题
/// </summary>
[StringValueTypeWriteConvertor<CardTheme>]
public class CardTheme : StringValueType, IStringValueType<CardTheme>
{
    private CardTheme(string value) : base(value) { }

    /// <summary>
    /// 默认
    /// </summary>
    public static readonly CardTheme Default = new("default");
    
    /// <summary>
    /// 灰色
    /// </summary>
    public static readonly CardTheme Grey = new("grey");
    
    /// <summary>
    /// 红色
    /// </summary>
    public static readonly CardTheme Red = new("red");
    
    /// <summary>
    /// 橙色
    /// </summary>
    public static readonly CardTheme Orange = new("orange");
    
    /// <summary>
    /// 黄色
    /// </summary>
    public static readonly CardTheme Yellow = new("yellow");
    
    /// <summary>
    /// 绿色
    /// </summary>
    public static readonly CardTheme Green = new("green");
    
    /// <summary>
    /// 靛青
    /// </summary>
    public static readonly CardTheme Indigo = new("indigo");
    
    /// <summary>
    /// 蓝色
    /// </summary>
    public static readonly CardTheme Blue = new("blue");
    
    /// <summary>
    /// 紫色
    /// </summary>
    public static readonly CardTheme Purple = new("purple");
    
    /// <summary>
    /// 黑色
    /// </summary>
    public static readonly CardTheme Black = new("black");
    
    public static IEnumerable<CardTheme> SupportedValues
    {
        get
        {
            yield return Default;
            yield return Grey;
            yield return Red;
            yield return Orange;
            yield return Yellow;
            yield return Green;
            yield return Indigo;
            yield return Blue;
            yield return Purple;
            yield return Black;
        }
    }
}
