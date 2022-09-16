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

using System.Text;

namespace DodoHosted.Base.App.Types;

public struct DodoEmoji
{
    public int EmojiId { get; }
    public string Emoji { get; }
    
    public static int GetEmojiId(string emoji)
        => emoji.EnumerateRunes().First().Value;

    public static string GetEmoji(int emojiId)
        => new Rune(emojiId).ToString();

    public DodoEmoji(string emoji)
    {
        Emoji = emoji;
        EmojiId = GetEmojiId(emoji);
    }
    
    public DodoEmoji(int emojiId)
    {
        EmojiId = emojiId;
        Emoji = GetEmoji(emojiId);
    }
}
