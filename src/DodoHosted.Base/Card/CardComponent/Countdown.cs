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
public record Countdown(string? Title, long EndTime, CountdownStyle Style) : ICardComponent
{
    public Countdown() : this(null, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), CountdownStyle.Hour) { }
    public Countdown(long endTime) : this(null, endTime, CountdownStyle.Hour) { }
    public Countdown(long endTime, CountdownStyle style) : this(null, endTime, style) { }
    public Countdown(string title, long endTime) : this(title, endTime, CountdownStyle.Hour) { }
    public Countdown(DateTimeOffset endTime) : this(null, endTime.ToUnixTimeMilliseconds(), CountdownStyle.Hour) { }
    public Countdown(DateTimeOffset endTime, CountdownStyle style) : this(null, endTime.ToUnixTimeMilliseconds(), style) { }
    public Countdown(string title, DateTimeOffset endTime) : this(title, endTime.ToUnixTimeMilliseconds(), CountdownStyle.Hour) { }
    public Countdown(string title, DateTimeOffset endTime, CountdownStyle style) : this(title, endTime.ToUnixTimeMilliseconds(), style) { }
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.Countdown;

    /// <summary>
    /// 倒计时标题
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; } = Title;

    /// <summary>
    /// 显示样式
    /// </summary>
    [JsonPropertyName("style")]
    public CountdownStyle Style { get; set; } = Style;

    /// <summary>
    /// 结束时间戳
    /// </summary>
    [JsonPropertyName("endTime")]
    public long EndTime { get; set; } = EndTime;
}
