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
/// 多栏文本
/// </summary>
public record MultilineText(Paragraph Text) : ICardComponent
{
    /// <summary>
    /// 多栏文本
    /// </summary>
    /// <param name="fields">段落文本</param>
    public MultilineText(params Text[] fields) : this(new Paragraph(fields)) { }
    
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.MultilineText;

    /// <summary>
    /// 文本数据
    /// </summary>
    [JsonPropertyName("text")]
    public Paragraph Text { get; set; } = Text;
}
