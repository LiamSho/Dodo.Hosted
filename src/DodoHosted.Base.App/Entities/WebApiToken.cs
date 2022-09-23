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

using DodoHosted.Base.App.Helpers;
using MongoDB.Bson.Serialization.Attributes;

namespace DodoHosted.Base.App.Entities;

/// <summary>
/// Web API Token
/// </summary>
public record WebApiToken(string Name)
{
    [BsonId]
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// Token 名称
    /// </summary>
    public string Name { get; init; } = Name;

    /// <summary>
    /// Token 值
    /// </summary>
    public string Token { get; init; } = TokenHelper.GenerateToken();
}
