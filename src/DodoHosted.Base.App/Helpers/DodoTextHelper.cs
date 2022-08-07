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

namespace DodoHosted.Base.App.Helpers;

// ReSharper disable ConvertIfStatementToReturnStatement

public static class DodoTextHelper
{
    public static string? ExtractChannelId(this string channelText, bool allowAsterisk = false)
    {
        if (channelText.StartsWith("<#") && channelText.EndsWith(">"))
        {
            return channelText[2..^1];
        }

        if (channelText == "*" && allowAsterisk)
        {
            return channelText;
        }

        if (long.TryParse(channelText, out _))
        {
            return channelText;
        }

        return null;
    }

    public static string? ExtractMemberId(this string memberText)
    {
        if (memberText.StartsWith("<@!") && memberText.EndsWith(">"))
        {
            return memberText[3..^1];
        }
        
        if (long.TryParse(memberText, out _))
        {
            return memberText;
        }

        return null;
    }
}
