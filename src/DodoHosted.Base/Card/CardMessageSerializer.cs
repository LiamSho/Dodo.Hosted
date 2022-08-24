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
using DodoHosted.Base.Attributes;
using DodoHosted.Base.Card.BaseComponent;
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

    /// <summary>
    /// 序列化模型，获取表单类
    /// </summary>
    /// <param name="title">表单标题</param>
    /// <typeparam name="T">模型类</typeparam>
    /// <returns></returns>
    public static Form SerializeFormData<T>(string title) where T : class
    {
        return SerializeFormData(title, typeof(T));
    }

    /// <summary>
    /// 序列化模型，获取表单类
    /// </summary>
    /// <param name="title">表单标题</param>
    /// <param name="type">模型类型</param>
    /// <returns></returns>
    public static Form SerializeFormData(string title, Type type)
    {
        var properties = type
            .GetProperties()
            .Where(x => x.PropertyType == typeof(string))
            .Select(x => (x,
                x.GetCustomAttribute<FormAttribute>(),
                x.GetCustomAttribute<FormBindAttribute>(),
                x.GetCustomAttribute<FormLimitAttribute>()))
            .Where(x => x.Item2 is not null && x.Item3 is not null)
            .Select(x => (x.Item2!, x.Item3!, x.Item4 ?? new FormLimitAttribute(0, 4000)));

        var inputs = new List<Input>();

        foreach (var (form, formBind, formLimit) in properties)
        {
            inputs.Add(new Input
            {
                Key = formBind.Id,
                Title = form.Title,
                Placeholder = form.Placeholder,
                MinChar = formLimit.MinCharacters,
                MaxChar = formLimit.MaxCharacters,
                Rows = formLimit.Rows
            });
        }

        return new Form { Title = title, Elements = inputs };
    }
    
    /// <summary>
    /// 反序列化表单信息
    /// </summary>
    /// <param name="formData">接收到的表单数据</param>
    /// <typeparam name="T">表单实体类</typeparam>
    /// <returns></returns>
    /// <remarks>
    /// 表单实体类必须包含无参构造函数
    /// </remarks>
    public static T DeserializeFormData<T>(IReadOnlyCollection<MessageModelFormData> formData) where T : class, new()
    {
        var properties =  typeof(T)
            .GetProperties()
            .Where(x => x.PropertyType == typeof(string))
            .Select(x => (x, x.GetCustomAttribute<FormBindAttribute>()))
            .Where(x => x.Item2 is not null)
            .Select(x => (x.x, x.Item2!));

        var model = new T();
    
        foreach (var (p, a) in properties)
        {
            var value = formData
                .FirstOrDefault(x => x.Key == a.Id)?
                .value ?? string.Empty;
            p.SetValue(model, value);
        }

        return model;
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
