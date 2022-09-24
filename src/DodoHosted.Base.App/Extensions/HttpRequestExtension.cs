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
using Microsoft.AspNetCore.Http;

namespace DodoHosted.Base.App.Extensions;

public static class HttpRequestExtension
{
    public static string ReadBodyAsString(this HttpRequest httpRequest)
    {
        var sr = new StreamReader(httpRequest.Body);
        var str = sr.ReadToEnd();
        sr.Close();

        return str;
    }
    
    public static async Task<string> ReadBodyAsStringAsync(this HttpRequest httpRequest)
    {
        var sr = new StreamReader(httpRequest.Body);
        var str = await sr.ReadToEndAsync();
        sr.Close();

        return str;
    }
    
    public static T? ReadBodyAsJson<T>(this HttpRequest httpRequest, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.Deserialize<T>(httpRequest.Body, options);
    }
    
    public static ValueTask<T?> ReadBodyAsJsonAsync<T>(this HttpRequest httpRequest, JsonSerializerOptions? options = null)
    {
        return JsonSerializer.DeserializeAsync<T>(httpRequest.Body, options);
    }
}
