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

// ReSharper disable InconsistentNaming
namespace DodoHosted.Lib.Plugin;

public static class PluginApiLevel
{
    private const int CURRENT_API_LEVEL = 1;
    private const int MINIMUM_COMPATIBLE_API_LEVEL = 1;

    public static int CurrentApiLevel => CURRENT_API_LEVEL;
    
    public static bool IsCompatible(int apiLevel) => apiLevel is >= MINIMUM_COMPATIBLE_API_LEVEL and <= CURRENT_API_LEVEL;
    public static string GetApiLevelString() => $"[{MINIMUM_COMPATIBLE_API_LEVEL, CURRENT_API_LEVEL}]";
}
