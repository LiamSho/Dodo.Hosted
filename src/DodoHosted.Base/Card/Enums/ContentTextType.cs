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
/// 文本类型
/// </summary>
[StringValueTypeWriteConvertor<ContentTextType>]
public class ContentTextType : StringValueType, IStringValueType<ContentTextType>
{
    private ContentTextType(string value) : base(value) { }
    
    /// <summary>
    /// 普通文本
    /// </summary>
    public static ContentTextType PlainText => new("plain-text");
    
    /// <summary>
    /// Markdown 文本
    /// </summary>
    public static ContentTextType DodoMarkdown => new("dodo-md");
    
    public static IEnumerable<ContentTextType> SupportedValues
    {
        get
        {
            yield return PlainText;
            yield return DodoMarkdown;
        }
    }
}
