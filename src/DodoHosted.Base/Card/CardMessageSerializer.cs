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

using DoDo.Open.Sdk.Models.Messages;

namespace DodoHosted.Base.Card;

public static class CardMessageSerializer
{
    /// <summary>
    /// 序列化卡片消息，用以发送卡片消息
    /// </summary>
    /// <param name="cardMessage"></param>
    /// <returns><see cref="MessageBodyCard"/></returns>
    public static MessageBodyCard Serialize(CardMessage cardMessage)
        => new()
        {
            Content = cardMessage.Content,
            Card = new DoDo.Open.Sdk.Models.Messages.Card
            {
                Title = cardMessage.Card.Title,
                Theme = cardMessage.Card.Theme,
                Type = cardMessage.Card.Type,
                Components = cardMessage.Card.Components
                    .Select(x => (object)x).ToList()
            }
        };
}
