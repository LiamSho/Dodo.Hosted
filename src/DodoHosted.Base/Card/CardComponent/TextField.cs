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

using System.Text.Json.Serialization;
using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.Card.Enums;

namespace DodoHosted.Base.Card.CardComponent;

/// <summary>
/// 文本
/// </summary>
public record TextFiled(Text Text) : ICardComponent
{
    /// <summary>
    /// 文本
    /// </summary>
    /// <param name="content">文本内容</param>
    /// <param name="type">文本类型，默认为 <see cref="ContentTextType.DodoMarkdown"/></param>
    public TextFiled(string content, ContentTextType? type = null)
        : this(new Text(content, type ?? ContentTextType.DodoMarkdown)) { }
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.Text;

    /// <summary>
    /// 文本数据
    /// </summary>
    [JsonPropertyName("text")]
    public Text Text { get; set; } = Text;
}
