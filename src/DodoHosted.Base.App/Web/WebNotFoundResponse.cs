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
using DodoHosted.Base.App.Enums;

namespace DodoHosted.Base.App.Web;

public record WebNotFoundResponse
{
    public WebNotFoundResponse(WebNotFoundType type)
    {
        Message = type.ToString();
        Type = (int)type;
    }
    
    [JsonPropertyName("message")]
    public string Message { get; init; }
    
    [JsonPropertyName("type")]
    public int Type { get; init; }
}
