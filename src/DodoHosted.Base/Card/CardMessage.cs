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

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace DodoHosted.Base.Card;

/// <summary>
/// 卡片消息
/// </summary>
public record CardMessage
{
    public CardMessage() { }
    
    [SetsRequiredMembers]
    public CardMessage(Card card)
    {
        Content = string.Empty;
        Card = card;
    }

    [SetsRequiredMembers]
    public CardMessage(string content, Card card)
    {
        Content = content;
        Card = card;
    }
    
    /// <summary>
    /// 附加文本，支持Markdown语法、菱形语法
    /// </summary>
    [JsonPropertyName("content")]
    public required string Content { get; set; }
    
    /// <summary>
    /// 卡片，限制10000个字符，支持 Markdown 语法，不支持菱形语法
    /// </summary>
    [JsonPropertyName("card")]
    public required Card Card { get; set; }

    /// <summary>
    /// 向组件容器中新增一个组件
    /// </summary>
    /// <param name="component"></param>
    public void AddComponent(ICardComponent component)
    {
        Card.Components.Add(component);
    }
}
