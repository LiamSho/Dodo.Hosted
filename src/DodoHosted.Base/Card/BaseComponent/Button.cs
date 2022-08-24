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
public record Button(string Name, ButtonAction Click, ButtonColor Color, string? InteractCustomId = null, Form? Form = null) : IAccessoryComponent
{
    public Button() : this(string.Empty, new ButtonAction(ButtonActionType.CopyContent), ButtonColor.Default) { }
    public Button(string name, string url) : this(name, new ButtonAction(ButtonActionType.LinkUrl, url), ButtonColor.Default) { }
    public Button(string name, ButtonAction action) : this(name, action, ButtonColor.Default) { }
    public Button(string name, string url, ButtonColor color) : this(name, new ButtonAction(ButtonActionType.LinkUrl, url), color) { }
    public Button(string name, string interactCustomId, string formTitle, Type modelType)
        : this(name, new ButtonAction(ButtonActionType.Form), ButtonColor.Default, interactCustomId,
            CardMessageSerializer.SerializeFormData(formTitle, modelType)) { }
    public Button(string name, ButtonColor color, string interactCustomId, string formTitle, Type modelType)
        : this(name, new ButtonAction(ButtonActionType.Form), color, interactCustomId,
            CardMessageSerializer.SerializeFormData(formTitle, modelType)) { }
    
    [JsonPropertyName("type")]
    public BaseComponentType Type => BaseComponentType.Button;

    /// <summary>
    /// 自定义按钮 ID
    /// </summary>
    [JsonPropertyName("interactCustomId")]
    public string? InteractCustomId { get; set; } = InteractCustomId;

    /// <summary>
    /// 按钮点击动作
    /// </summary>
    [JsonPropertyName("click")]
    public ButtonAction Click { get; set; } = Click;

    /// <summary>
    /// 按钮颜色
    /// </summary>
    [JsonPropertyName("color")]
    public ButtonColor Color { get; set; } = Color;

    /// <summary>
    /// 按钮名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = Name;

    /// <summary>
    /// 回传表单，仅当按钮点击动作 <see cref="ButtonAction.Action"/> 为 <see cref="ButtonActionType.Form"/> 时需要填写
    /// </summary>
    [JsonPropertyName("form")]
    public Form? Form { get; set; } = Form;
}

/// <summary>
/// 按钮点击动作
/// </summary>
public record ButtonAction(ButtonActionType Action, string Value = "")
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
