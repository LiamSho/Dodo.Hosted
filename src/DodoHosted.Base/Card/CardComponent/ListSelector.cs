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
    /// <summary>
    /// 列表选择器
    /// </summary>
    /// <param name="interactCustomId">交互 ID</param>
    /// <param name="placeholder">占位符</param>
    /// <param name="min">最少选择数目，默认为 1</param>
    /// <param name="max">最多选择数目，默认为 1</param>
    /// <param name="elements">选择组件</param>
    public ListSelector(string interactCustomId, string? placeholder = null, int? min = null, int? max = null, params ListSelectorOption[] elements)
        : this(min ?? 1, max ?? 1, elements.ToList(), placeholder, interactCustomId) { }
    
    /// <summary>
    /// 列表选择器
    /// </summary>
    /// <param name="modelType">模型类型</param>
    /// <param name="interactCustomId">交互 ID</param>
    /// <param name="placeholder">占位符</param>
    /// <param name="min">最少选择数目，默认为 1</param>
    /// <param name="max">最多选择数目，默认为 1</param>
    public ListSelector(Type modelType, string interactCustomId, string? placeholder = null, int? min = null, int? max = null)
        : this(min ?? 1, max ?? 1, modelType.SerializeListSelectorOptions(), placeholder, interactCustomId) { }
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.ListSelector;

    /// <summary>
    /// 交互自定义 ID
    /// </summary>
    [JsonPropertyName("interactCustomId")]
    public string? InteractCustomId { get; set; } = InteractCustomId;

    /// <summary>
    /// 输入框提示
    /// </summary>
    [JsonPropertyName("placeholder")]
    public string? Placeholder { get; set; } = Placeholder;

    /// <summary>
    /// 数据列表
    /// </summary>
    [JsonPropertyName("elements")]
    public List<ListSelectorOption> Elements { get; set; } = Elements;

    /// <summary>
    /// 最少选中个数
    /// </summary>
    [JsonPropertyName("min")]
    public int Min { get; set; } = Min;

    /// <summary>
    /// 最大选中个数
    /// </summary>
    [JsonPropertyName("max")]
    public int Max { get; set; } = Max;
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
