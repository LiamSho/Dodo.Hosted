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
/// 文字 + 模块
/// </summary>
public record TextWithModule : ICardComponent
{
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.TextWithModule;
    
    /// <summary>
    /// 对齐方式
    /// </summary>
    [JsonPropertyName("align")]
    public required TextWithModuleAlign Align { get; set; }
    
    /// <summary>
    /// 文本
    /// </summary>
    [JsonPropertyName("text")]
    public required ITextComponent Text { get; set; }
    
    /// <summary>
    /// 附件
    /// </summary>
    [JsonPropertyName("accessory")]
    public required IAccessoryComponent Accessory { get; set; }
}
