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
using DodoHosted.Base.Card.CardComponent;
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
    public static MessageBodyCard Serialize(this CardMessage cardMessage)
    {
        return new MessageBodyCard
        {
            Content = cardMessage.Content,
            Card = new MessageModelCard
            {
                Title = cardMessage.Card.Title,
                Theme = cardMessage.Card.Theme,
                Type = cardMessage.Card.Type,
                Components = cardMessage.Card.Components
                    .Select(x => (object)x).ToList()
            }
        };
    }

    /// <summary>
    /// 反序列化卡片消息
    /// </summary>
    /// <param name="messageBodyCard"></param>
    /// <returns></returns>
    /// <exception cref="CardMessageSerializeException"></exception>
    public static CardMessage Deserialize(this MessageBodyCard messageBodyCard)
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

            var componentType = GetComponentType(jsonDocument);
            
            ICardComponent? cardComponent = null;
            try
            {
                var deserialized = jsonDocument.Deserialize(componentType)!;
                cardComponent = deserialized as ICardComponent;
            }
            catch (Exception)
            {
                // Ignore
            }

            if (cardComponent is null)
            {
                throw new CardMessageSerializeException($"无法反序列化卡片组件: {jsonDocument.ToString()}");
            }
            
            card.Card.Components.Add(cardComponent);
        }

        return card;
    }

    /// <summary>
    /// 序列化模型，获取表单类
    /// </summary>
    /// <param name="title">表单标题</param>
    /// <typeparam name="T">模型类</typeparam>
    /// <returns></returns>
    public static Form SerializeFormData<T>(this string title) where T : class
    {
        return typeof(T).SerializeFormData(title);
    }

    /// <summary>
    /// 序列化模型，获取表单类
    /// </summary>
    /// <param name="title">表单标题</param>
    /// <param name="type">模型类型</param>
    /// <returns></returns>
    public static Form SerializeFormData(this Type type, string title)
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
    public static T DeserializeFormData<T>(this IReadOnlyCollection<MessageModelFormData> formData) where T : class, new()
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
                .Value ?? string.Empty;
            p.SetValue(model, value);
        }

        return model;
    }

    /// <summary>
    /// 序列化列表选择器，获取列表选择器的选项
    /// </summary>
    /// <typeparam name="T">枚举类型</typeparam>
    /// <returns></returns>
    public static List<ListSelectorOption> SerializeListSelectorOptions<T>() where T : Enum
    {
        return SerializeListSelectorOptions(typeof(T));
    }
    
    /// <summary>
    /// 序列化列表选择器，获取列表选择器的选项
    /// </summary>
    /// <param name="enumType">枚举类型</param>
    /// <returns></returns>
    public static List<ListSelectorOption> SerializeListSelectorOptions(this Type enumType)
    {
        var options = enumType
            .GetFields()
            .Select(x => x.GetCustomAttribute<ListSelectorAttribute>())
            .Where(x => x is not null)
            .Select(x => new ListSelectorOption(x!.Name, x.Description));

        return options.ToList();
    }
    
    /// <summary>
    /// 反序列化列表选择器，获取枚举列表
    /// </summary>
    /// <param name="listData">列表选择器数据</param>
    /// <typeparam name="T">源类型</typeparam>
    /// <returns></returns>
    public static List<T> DeserializeListSelectorOptions<T>(this IEnumerable<MessageModelListData> listData) where T : Enum
    {
        var fields = typeof(T)
            .GetFields()
            .Select(x => (x, x.GetCustomAttribute<ListSelectorAttribute>()))
            .Where(x => x.Item2 is not null)
            .Select(x => (x.x, x.Item2!))
            .ToList();

        var list = new List<T>();
        var enumValues = typeof(T).GetEnumValues();
        
        foreach (var data in listData)
        {
            var (p, _) = fields.FirstOrDefault(x => x.Item2.Name == data.Name);
            var index = (int)p.GetRawConstantValue()!;
            var enumValue = (T)enumValues.GetValue(index)!;
            list.Add(enumValue);
        }

        return list;
    }

    private static readonly List<CardComponentDescription> s_componentTypes =
        typeof(CardComponentType)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Select(x => new CardComponentDescription(
                (CardComponentType)x.GetValue(null)!, x.GetCustomAttribute<StringValueTypeRefAttribute>()!))
            .ToList();

    private static Type GetComponentType(JsonElement element)
    {
        var typeString = element.GetProperty("type").GetString();
        var type = StringValueType.Parse<CardComponentType>(typeString);
        if (type is null)
        {
            throw new CardMessageSerializeException($"未知的卡片组件类型 {typeString ?? "NULL"}");
        }

        if (type != "section")
        {
            return s_componentTypes.FirstOrDefault(x => x.Type == type)!.Attr.Type;
        }

        var hasAccessory = element.TryGetProperty("accessory", out _);
        if (hasAccessory)
        {
            return typeof(TextWithModule);
        }
            
        var isParagraph = element.GetProperty("text").TryGetProperty("fields", out _);
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (isParagraph)
        {
            return typeof(MultilineText);
        }

        return typeof(TextFiled);
    }

    private record CardComponentDescription(CardComponentType Type, StringValueTypeRefAttribute Attr);
}
