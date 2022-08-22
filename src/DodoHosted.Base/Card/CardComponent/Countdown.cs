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

namespace DodoHosted.Base.Card.CardComponent;

/// <summary>
/// 倒计时
/// </summary>
public record Countdown : ICardComponent
{
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.Countdown;

    /// <summary>
    /// 倒计时标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// 显示样式
    /// </summary>
    [JsonPropertyName("style")]
    public required CountdownStyle Style { get; set; }
    
    /// <summary>
    /// 结束时间戳
    /// </summary>
    [JsonPropertyName("endTime")]
    public required long EndTime { get; set; }
}
