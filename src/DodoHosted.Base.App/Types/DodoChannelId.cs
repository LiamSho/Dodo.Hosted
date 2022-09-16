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

namespace DodoHosted.Base.App.Types;

public struct DodoChannelId
{
    public string Value { get; }
    public bool Valid { get; }

    public DodoChannelId(string value)
    {
        var extractChannelId = Extract(value);

        if (extractChannelId is null)
        {
            Value = string.Empty;
            Valid = false;
        }
        else
        {
            Value = extractChannelId;
            Valid = true;
        }
    }
    
    public DodoChannelId EnsureValid()
    {
        if (Valid)
        {
            return this;
        }

        throw new ArgumentException("非法的频道 ID");
    }
    
    public static string? Extract(string channelText, bool allowAsterisk = false)
    {
        if (channelText.StartsWith("<#") && channelText.EndsWith(">"))
        {
            return channelText[2..^1];
        }

        if (channelText == "*" && allowAsterisk)
        {
            return channelText;
        }

        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (long.TryParse(channelText, out _))
        {
            return channelText;
        }

        return null;
    }
}
