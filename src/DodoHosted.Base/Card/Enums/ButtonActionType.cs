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
/// 按钮动作类型
/// </summary>
[StringValueTypeWriteConvertor<ButtonActionType>]
public class ButtonActionType : StringValueType, IStringValueType<ButtonActionType>
{
    private ButtonActionType(string value) : base(value) { }

    /// <summary>
    /// 跳转链接
    /// </summary>
    public static readonly ButtonActionType LinkUrl = new("link_url");
    
    /// <summary>
    /// 回传参数
    /// </summary>
    public static readonly ButtonActionType CallBack = new("call_back");
    
    /// <summary>
    /// 复制内容
    /// </summary>
    public static readonly ButtonActionType CopyContent = new("copy_content");
    
    /// <summary>
    /// 回传表单
    /// </summary>
    public static readonly ButtonActionType Form = new("form");
    
    public static IEnumerable<ButtonActionType> SupportedValues
    {
        get
        {
            yield return LinkUrl;
            yield return CallBack;
            yield return CopyContent;
            yield return Form;
        }
    }
}
