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
/// 输入框
/// </summary>
public record Input
{
    [JsonPropertyName("type")]
    public BaseComponentType Type => BaseComponentType.Input;

    /// <summary>
    /// 选项自定义ID
    /// </summary>
    [JsonPropertyName("key")]
    public required string Key { get; set; }

    /// <summary>
    /// 选项名称
    /// </summary>
    [JsonPropertyName("title")]
    public required string Title { get; set; }

    /// <summary>
    /// 输入框高度，填1表示单行，最多4行
    /// </summary>
    [JsonPropertyName("rows")]
    public required int Rows { get; set; }

    /// <summary>
    /// 输入框提示
    /// </summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }

    /// <summary>
    /// 最小字符数，介于0~4000
    /// </summary>
    [JsonPropertyName("minChar")]
    public required int MinChar { get; set; }

    /// <summary>
    /// 最大字符数，介于1~4000，且最大字符数不得小于最小字符数
    /// </summary>
    [JsonPropertyName("maxChar")]
    public required int MaxChar { get; set; }
}
