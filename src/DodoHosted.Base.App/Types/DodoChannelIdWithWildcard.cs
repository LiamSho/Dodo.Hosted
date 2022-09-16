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

public struct DodoChannelIdWithWildcard
{
    public string Value { get; }
    public bool Valid { get; }

    public DodoChannelIdWithWildcard(string value)
    {
        var extractChannelId = DodoChannelId.Extract(value, true);

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
    
    public DodoChannelIdWithWildcard EnsureValid()
    {
        if (Valid)
        {
            return this;
        }

        throw new ArgumentException("非法的频道 ID");
    }
}
