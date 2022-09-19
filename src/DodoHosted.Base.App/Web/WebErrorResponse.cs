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

namespace DodoHosted.Base.App.Web;

public record WebErrorResponse
{
    public WebErrorResponse(Exception ex)
    {
        ExceptionType = ex.GetType().FullName!;
        Message = ex.Message;
    }
    
    [JsonPropertyName("type")]
    public string ExceptionType { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; }
}
