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
/// 按钮
/// </summary>
public record Button : IAccessoryComponent
{
    [JsonPropertyName("type")]
    public BaseComponentType Type => BaseComponentType.Button;
    
    /// <summary>
    /// 自定义按钮 ID
    /// </summary>
    [JsonPropertyName("interactCustomId")]
    public string? InteractCustomId { get; set; }
    
    /// <summary>
    /// 按钮点击动作
    /// </summary>
    [JsonPropertyName("click")]
    public required ButtonAction Click { get; set; }

    /// <summary>
    /// 按钮颜色
    /// </summary>
    [JsonPropertyName("color")]
    public ButtonColor Color { get; set; } = ButtonColor.Default;
    
    /// <summary>
    /// 按钮名称
    /// </summary>
    [JsonPropertyName("name")]
    public required string Name { get; set; }
    
    /// <summary>
    /// 回传表单，仅当按钮点击动作 <see cref="ButtonAction.Action"/> 为 <see cref="ButtonActionType.Form"/> 时需要填写
    /// </summary>
    [JsonPropertyName("form")]
    public Form? Form { get; set; }
}

/// <summary>
/// 按钮点击动作
/// </summary>
public record ButtonAction(ButtonActionType Action, string Value)
{
    /// <summary>
    /// 按钮动作类型
    /// </summary>
    [JsonPropertyName("action")]
    public ButtonActionType Action { get; set; } = Action;

    /// <summary>
    /// 动作值
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = Value;
}
