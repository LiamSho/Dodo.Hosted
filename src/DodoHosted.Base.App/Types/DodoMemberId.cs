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

public struct DodoMemberId
{
    public string Value { get; }
    public bool Valid { get; }

    public DodoMemberId(string value)
    {
        var extractMemberId = Extract(value);

        if (extractMemberId is null)
        {
            Value = string.Empty;
            Valid = false;
        }
        else
        {
            Value = extractMemberId;
            Valid = true;
        }
    }

    public DodoMemberId EnsureValid()
    {
        if (Valid)
        {
            return this;
        }

        throw new ArgumentException("非法的成员 ID");
    }
    
    public static string? Extract(string memberText)
    {
        if (memberText.StartsWith("<@!") && memberText.EndsWith(">"))
        {
            return memberText[3..^1];
        }
        
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if (long.TryParse(memberText, out _))
        {
            return memberText;
        }

        return null;
    }
}
