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

namespace DodoHosted.Lib.Plugin.Extensions;

public static class DictionaryExtension
{
    public static bool TryGetValueByMultipleKey<T, K>(this IReadOnlyDictionary<T, K> dictionary, IEnumerable<T> keys, out K? value) where T : notnull
    {
        foreach (var key in keys)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
