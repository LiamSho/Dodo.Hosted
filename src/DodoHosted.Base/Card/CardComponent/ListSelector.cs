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
/// 列表选择器
/// </summary>
public record ListSelector : ICardComponent
{
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.ListSelector;
    
    /// <summary>
    /// 交互自定义 ID
    /// </summary>
    [JsonPropertyName("interactCustomId")]
    public string? InteractCustomId { get; set; }
    
    /// <summary>
    /// 输入框提示
    /// </summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; }
    
    /// <summary>
    /// 数据列表
    /// </summary>
    [JsonPropertyName("elements")]
    public required List<ListSelectorOption> Elements { get; set; }
    
    /// <summary>
    /// 最少选中个数
    /// </summary>
    [JsonPropertyName("min")]
    public required int Min { get; set; }
    
    /// <summary>
    /// 最大选中个数
    /// </summary>
    [JsonPropertyName("max")]
    public required int Max { get; set; }
}

/// <summary>
/// 列表选择器数据列表
/// </summary>
public record ListSelectorOption(string Name, string Description)
{
    /// <summary>
    /// 选项名
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = Name;

    /// <summary>
    /// 选项描述
    /// </summary>
    [JsonPropertyName("desc")]
    public string Description { get; set; } = Description;
}
