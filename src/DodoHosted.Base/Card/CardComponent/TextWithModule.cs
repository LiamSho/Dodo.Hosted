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
using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.Card.Enums;

namespace DodoHosted.Base.Card.CardComponent;

/// <summary>
/// 文字 + 模块
/// </summary>
public record TextWithModule(ITextComponent Text, IAccessoryComponent Accessory, TextWithModuleAlign Align) : ICardComponent
{
    public TextWithModule() : this(new Text(string.Empty), new Image(string.Empty), TextWithModuleAlign.Left) { }
    public TextWithModule(ITextComponent text, IAccessoryComponent accessory) : this(text, accessory, TextWithModuleAlign.Left) { }
    public TextWithModule(string markdownText, string imageSource) : this(new Text(markdownText), new Image(imageSource), TextWithModuleAlign.Left) { }
    public TextWithModule(string markdownText, string imageSource, TextWithModuleAlign align) : this(new Text(markdownText), new Image(imageSource), align) { }
    public TextWithModule(string markdownText, IAccessoryComponent accessory) : this(new Text(markdownText), accessory, TextWithModuleAlign.Left) { }
    public TextWithModule(string markdownText, IAccessoryComponent accessory, TextWithModuleAlign align) : this(new Text(markdownText), accessory, align) { }
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.TextWithModule;

    /// <summary>
    /// 对齐方式
    /// </summary>
    [JsonPropertyName("align")]
    public TextWithModuleAlign Align { get; set; } = Align;

    /// <summary>
    /// 文本
    /// </summary>
    [JsonPropertyName("text")]
    public ITextComponent Text { get; set; } = Text;

    /// <summary>
    /// 附件
    /// </summary>
    [JsonPropertyName("accessory")]
    public IAccessoryComponent Accessory { get; set; } = Accessory;
}
