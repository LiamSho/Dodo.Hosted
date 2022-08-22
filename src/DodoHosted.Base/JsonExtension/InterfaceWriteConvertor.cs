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

namespace DodoHosted.Base.JsonExtension;

public class InterfaceWriteConverter<T> : JsonConverter<T> where T : class
{

    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
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
