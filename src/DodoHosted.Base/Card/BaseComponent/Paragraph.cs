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

namespace DodoHosted.Base.Card.BaseComponent;

/// <summary>
/// 段落
/// </summary>
public record Paragraph(int Column, List<Text> Fields) : ITextComponent
{
    /// <summary>
    /// 段落
    /// </summary>
    /// <param name="fields">段落文本</param>
    public Paragraph(params Text[] fields) : this(fields.Length, fields.ToList()) { }
    
    [JsonPropertyName("type")]
    public string Type => BaseComponentType.Paragraph;

    /// <summary>
    /// 栏数，2~6栏
    /// </summary>
    [JsonPropertyName("cols")]
    public int Column { get; set; } = Column;

    /// <summary>
    /// 数据列表
    /// </summary>
    [JsonPropertyName("fields")]
    public List<Text> Fields { get; set; } = Fields;
}
