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
/// 通用文本组件
/// </summary>
public record Text : ITextComponent, IRemarkElementComponent
{
    /// <summary>
    /// 文本类型
    /// </summary>
    [JsonPropertyName("type")]
    public required ContentTextType Type { get; set; }
    
    /// <summary>
    /// 文本内容
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }
}
