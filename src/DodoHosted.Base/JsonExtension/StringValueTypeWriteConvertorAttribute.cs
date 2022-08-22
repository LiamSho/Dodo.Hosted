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

namespace DodoHosted.Base.JsonExtension;

[AttributeUsage(AttributeTargets.Class)]
public class StringValueTypeWriteConvertorAttribute<T> : JsonConverterAttribute where T : StringValueType
{
    public StringValueTypeWriteConvertorAttribute() : base(typeof(StringValueTypeWriteConvertor<T>)) { }
}
