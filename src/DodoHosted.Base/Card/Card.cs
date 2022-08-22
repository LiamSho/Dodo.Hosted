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

namespace DodoHosted.Base.Card;

/// <summary>
/// 卡片
/// </summary>
public record Card
{
    [JsonPropertyName("type")]
    public string Type => "card";
    
    /// <summary>
    /// 卡片内容组件
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 卡片风格
    /// </summary>
    [JsonPropertyName("components")]
    public required List<ICardComponent> Components { get; set; }
    
    /// <summary>
    /// 卡片标题，只支持普通文本，可以为空字符串
    /// </summary>
    [JsonPropertyName("theme")]
    public required CardTheme Theme { get; set; }
}
