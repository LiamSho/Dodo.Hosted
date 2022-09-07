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

using System.Text.Json;
using System.Text.Json.Serialization;
using DodoHosted.Base.Card;
using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.Card.CardComponent;
using DodoHosted.Base.Card.Enums;

namespace DodoHosted.Base.JsonExtension;

public class InterfaceWriteConverter<T> : JsonConverter<T> where T : class
{

    private readonly Dictionary<string, Type> _possibleRemarkElementComponentTypes = new()
    {
        { ContentTextType.PlainText, typeof(Text) },
        { ContentTextType.DodoMarkdown, typeof(Text) },
        { CardComponentType.Image, typeof(Image) }
    };
    
    private readonly Dictionary<string, Type> _possibleTextComponentTypes = new()
    {
        { ContentTextType.PlainText, typeof(Text) },
        { ContentTextType.DodoMarkdown, typeof(Text) },
        { BaseComponentType.Paragraph, typeof(Paragraph) }
    };
    
    private readonly Dictionary<string, Type> _possibleAccessoryComponentTypes = new()
    {
        { CardComponentType.Image, typeof(Image) },
        { BaseComponentType.Button, typeof(Button) }
    };

    private Type GetDerivedType(string? typeString)
    {
        if (typeString is null)
        {
            throw new ArgumentNullException(nameof(typeString), "接口实现组件类型为空");
        }
        
        Dictionary<string, Type> possibleTypes;
        if (typeof(T) == typeof(IRemarkElementComponent))
        {
            possibleTypes = _possibleRemarkElementComponentTypes;
        }
        else if (typeof(T) == typeof(ITextComponent))
        {
            possibleTypes = _possibleTextComponentTypes;
        }
        else if (typeof(T) == typeof(IAccessoryComponent))
        {
            possibleTypes = _possibleAccessoryComponentTypes;
        }
        else
        {
            throw new NotSupportedException($"InterfaceWriteConverter 反序列化不支持接口类型 {typeof(T).FullName}");
        }
        
        if (possibleTypes.TryGetValue(typeString, out var type))
        {
            return type;
        }
        
        throw new JsonException($"接口 {typeof(T).FullName} 可能的实现中不包含组件类型 {typeString}");
    }
    
    public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonDocument = JsonDocument.ParseValue(ref reader);
        var typeString = jsonDocument.RootElement.GetProperty("type").GetString();
        var type = GetDerivedType(typeString);
        return jsonDocument.RootElement.Deserialize(type) as T;
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        switch (value)
        {
            case null:
                JsonSerializer.Serialize(writer, (T?)null, options);
                break;
            default:
                {
                    var type = value.GetType();
                    using var jsonDocument = JsonDocument.Parse(JsonSerializer.Serialize(value, type, options));
                    writer.WriteStartObject();

                    foreach (var element in jsonDocument.RootElement.EnumerateObject())
                    {
                        element.WriteTo(writer);
                    }

                    writer.WriteEndObject();
                    break;
                }
        }
    }
}
