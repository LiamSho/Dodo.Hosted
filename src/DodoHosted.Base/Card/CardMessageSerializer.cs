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

using System.Reflection;
using System.Text.Json;
using DoDo.Open.Sdk.Models.Messages;
using DodoHosted.Base.Card.Enums;
using DodoHosted.Base.Exceptions;

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

    [Obsolete("Not Fully Implemented")]
    public static CardMessage Deserialize(MessageBodyCard messageBodyCard)
    {
        var card = new CardMessage
        {
            Content = messageBodyCard.Content,
            Card = new Card
            {
                Title = messageBodyCard.Card.Title,
                Theme = StringValueType.Parse<CardTheme>(messageBodyCard.Card.Theme) ?? CardTheme.Default,
                Components = new List<ICardComponent>()
            }
        };

        var componentObjects = messageBodyCard.Card.Components;
        foreach (var componentObject in componentObjects)
        {
            var objectString = componentObject.ToString()!;
            var jsonDocument = JsonDocument.Parse(objectString).RootElement;

            var type = StringValueType.Parse<CardComponentType>(jsonDocument.GetProperty("type").GetString());
            if (type is null)
            {
                throw new CardMessageSerializeException("");
            }

            var componentType = GetCardComponentType(type);
            var component = (ICardComponent)jsonDocument.Deserialize(componentType)!;

            card.Card.Components.Add(component);
        }

        return card;
    }

    private static readonly PropertyInfo[] s_propertyInfos = typeof(CardComponentType).GetProperties(BindingFlags.Static);

    private static Type GetCardComponentType(CardComponentType type)
    {
        var p = s_propertyInfos.FirstOrDefault(x => x.GetConstantValue()?.ToString() == (string)type);
        if (p is null)
        {
            throw new CardMessageSerializeException("");
        }

        var attr = p.GetCustomAttribute<StringValueTypeRefAttribute>();
        if (attr is null)
        {
            throw new CardMessageSerializeException("");
        }
        
        return attr.Type;
    }
}
