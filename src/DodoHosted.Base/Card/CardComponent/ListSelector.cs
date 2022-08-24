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
public record ListSelector(int Min, int Max, List<ListSelectorOption> Elements, string? Placeholder = null, string? InteractCustomId = null) : ICardComponent
{
    public ListSelector() : this(1, 1, new List<ListSelectorOption>()) { }
    public ListSelector(List<ListSelectorOption> elements, string? placeholder = null, string? interactCustomId = null) 
        : this(1, 1, elements, placeholder, interactCustomId) { }
    public ListSelector(string? placeholder = null, string? interactCustomId = null, params ListSelectorOption[] elements) 
        : this(1, 1, elements.ToList(), placeholder, interactCustomId) { }
    public ListSelector(int min, int max, string? placeholder = null, string? interactCustomId = null, params ListSelectorOption[] elements)
        : this(min, max, elements.ToList(), placeholder, interactCustomId) { }
    public ListSelector(List<ListSelectorOption> elements,string? interactCustomId = null) 
        : this(1, 1, elements, null, interactCustomId) { }
    public ListSelector(string? interactCustomId = null, params ListSelectorOption[] elements) 
        : this(1, 1, elements.ToList(), null, interactCustomId) { }
    public ListSelector(int min, int max, string? interactCustomId = null, params ListSelectorOption[] elements)
        : this(min, max, elements.ToList(), null, interactCustomId) { }
    
    
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
    public List<ListSelectorOption> Elements { get; set; }
    
    /// <summary>
    /// 最少选中个数
    /// </summary>
    [JsonPropertyName("min")]
    public int Min { get; set; }
    
    /// <summary>
    /// 最大选中个数
    /// </summary>
    [JsonPropertyName("max")]
    public int Max { get; set; }
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
