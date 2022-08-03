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

using Microsoft.Extensions.Logging;

namespace DodoHosted.App;

public static class AppEnvs
{
    /// <summary>
    /// Dodo 机器人 Client ID
    /// </summary>
    public static string DodoBotClientId => ReadEnvironmentVariable(
        "DODO_SDK_BOT_CLIENT_ID",
        string.Empty);

    /// <summary>
    /// Dodo 机器人 Token
    /// </summary>
    public static string DodoBotToken => ReadEnvironmentVariable(
        "DODO_SDK_BOT_TOKEN",
        string.Empty);

    public static LogLevel DodoHostedOpenApiLogLevel =>
        ReadEnvironmentVariable("DODO_HOSTED_OPENAPI_LOG_LEVEL", "Debug") switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Debug
        };

    private static string ReadEnvironmentVariable(string key, string defaultValue) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))
            ? defaultValue
            : Environment.GetEnvironmentVariable(key)!;
}
