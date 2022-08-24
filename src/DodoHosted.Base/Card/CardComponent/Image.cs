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
/// 图片
/// </summary>
public record Image(string Source) : ICardComponent, IAccessoryComponent, IRemarkElementComponent
{
    public Image() : this(string.Empty) { }
    
    public CardComponentType Type => CardComponentType.Image;

    /// <summary>
    /// 图片地址
    /// </summary>
    [JsonPropertyName("src")]
    public string Source { get; set; } = Source;
}
