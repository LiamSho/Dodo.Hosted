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

using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.JsonExtension;

namespace DodoHosted.Base.Card.Enums;

/// <summary>
/// 基本组件类型
/// </summary>
[StringValueTypeWriteConvertor<BaseComponentType>]
public class BaseComponentType : StringValueType, IStringValueType<BaseComponentType>
{
    private BaseComponentType(string value) : base(value) { }
    
    /// <summary>
    /// 按钮
    /// </summary>
    [StringValueTypeRef(typeof(Button))]
    public static readonly BaseComponentType Button = new("button");
    
    /// <summary>
    /// 输入框
    /// </summary>
    [StringValueTypeRef(typeof(Input))]
    public static readonly BaseComponentType Input = new("input");
    
    /// <summary>
    /// 段落
    /// </summary>
    [StringValueTypeRef(typeof(Paragraph))]
    public static readonly BaseComponentType Paragraph = new("paragraph");

    public static IEnumerable<BaseComponentType> SupportedValues
    {
        get
        {
            yield return Button;
            yield return Input;
            yield return Paragraph;
        }
    }
}
