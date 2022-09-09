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
/// 多图
/// </summary>
public record ImageGroup(List<Image> Images) : ICardComponent
{
    /// <summary>
    /// 多图
    /// </summary>
    /// <param name="images">图片组件</param>
    public ImageGroup(params Image[] images) : this(images.ToList()) { }
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.ImageGroup;

    /// <summary>
    /// 图片列表
    /// </summary>
    [JsonPropertyName("elements")]
    public List<Image> Images { get; set; } = Images;
}
