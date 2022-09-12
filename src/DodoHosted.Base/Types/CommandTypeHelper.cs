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

namespace DodoHosted.Base.Types;

public static class CommandTypeHelper
{
    public static string GetFriendlyName(this Type type)
    {
        if (type == typeof(string))
        {
            return "字符串";
        }
        if (type == typeof(int) || type == typeof(int?))
        {
            return "整数";
        }
        if (type == typeof(long) || type == typeof(long?))
        {
            return "长整数";
        }
        if (type == typeof(double) || type == typeof(double?))
        {
            return "浮点数";
        }
        if (type == typeof(DodoEmoji) || type == typeof(DodoEmoji?))
        {
            return "Emoji";
        }
        if (type == typeof(DodoChannelId) || type == typeof(DodoChannelId?))
        {
            return "Dodo 频道";
        }
        if (type == typeof(DodoChannelIdWithWildcard) || type == typeof(DodoChannelIdWithWildcard?))
        {
            return "Dodo 频道 (可为通配符)";
        }
        if (type == typeof(DodoMemberId) || type == typeof(DodoMemberId?))
        {
            return "Dodo ID";
        }
        
        throw new ArgumentOutOfRangeException(nameof(type), "不支持的 Command 类型");
    }

    public static readonly Type[] SupportedBasicValueTypes =
    {
        typeof(string), typeof(int), typeof(long), typeof(double),
        typeof(int?), typeof(long?), typeof(double?)
    };

    public static readonly Type[] SupportedCmdOptionTypes =
    {
        typeof(string), typeof(int), typeof(long), typeof(double),
        typeof(DodoChannelId), typeof(DodoChannelIdWithWildcard), typeof(DodoMemberId), typeof(DodoEmoji),
        typeof(int?), typeof(long?), typeof(double?), typeof(DodoChannelId?), typeof(DodoChannelIdWithWildcard?),
        typeof(DodoMemberId?), typeof(DodoEmoji?)
    };

    public static readonly Type[] SupportedCmdOptionNullableTypes =
    {
        typeof(string), typeof(int?), typeof(long?), typeof(double?), typeof(DodoChannelId?), typeof(DodoChannelIdWithWildcard?),
        typeof(DodoMemberId?), typeof(DodoEmoji?)
    };
}
