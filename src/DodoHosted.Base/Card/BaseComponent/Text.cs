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
using DodoHosted.Base.Card.Enums;
using DodoHosted.Base.JsonExtension.Extra;

namespace DodoHosted.Base.Card.BaseComponent;

/// <summary>
/// 通用文本组件
/// </summary>
public record Text(string Content, ContentTextType Type) : ITextComponent, IRemarkElementComponent
{
    public Text() : this(string.Empty, ContentTextType.DodoMarkdown) { }
    
    public Text(string content) : this(content, ContentTextType.DodoMarkdown) { }

    /// <summary>
    /// 文本类型
    /// </summary>
    [JsonPropertyName("type")]
    [JsonConverter(typeof(ContentTextTypeConvertor))]
    public ContentTextType Type { get; set; } = Type;

    /// <summary>
    /// 文本内容
    /// </summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = Content;
}
