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

using System.Security.Cryptography;

namespace DodoHosted.Base.App.Helpers;

public static class TokenHelper
{
    /// <summary>
    /// 生成一个随机 Token
    /// </summary>
    /// <param name="length">Token 长度，默认为 128 Bytes</param>
    /// <returns></returns>
    public static string GenerateToken(int length = 128)
    {
        var rndBytes = RandomNumberGenerator.GetBytes(length);
        return Convert.ToBase64String(rndBytes);
    }
}
