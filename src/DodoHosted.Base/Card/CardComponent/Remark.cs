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
/// 备注
/// </summary>
public record Remark(List<IRemarkElementComponent> Elements) : ICardComponent
{
    public Remark() : this(new List<IRemarkElementComponent>()) { }
    public Remark(params IRemarkElementComponent[] elements) : this(elements.ToList()) { }
    
    [JsonPropertyName("type")]
    public CardComponentType Type => CardComponentType.Remark;

    /// <summary>
    /// 数据列表
    /// </summary>
    [JsonPropertyName("elements")]
    public List<IRemarkElementComponent> Elements { get; set; } = Elements;
}
