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
using Serilog;
using Serilog.Events;

namespace DodoHosted.App;

public static class AppEnvs
{
    /// <summary>
    /// Serilog 最低记录等级，默认为 Information
    /// </summary>
    public static LogEventLevel SerilogMinimumLevel => Enum.TryParse<LogLevel>(ReadEnvironmentVariable(
        "DODO_HOSTED_APP_LOGGER_MINIMUM_LEVEL", "Information"), out var level)
        ? level.ToLogEventLevel()
        : LogEventLevel.Information;
    
    /// <summary>
    /// Serilog 文件记录器保存路径，为空表示不使用
    /// </summary>
    public static string SerilogSinkToFile => ReadEnvironmentVariable(
        "DODO_HOSTED_APP_LOGGER_SINK_TO_FILE", string.Empty);

    /// <summary>
    /// Serilog 文件记录器滚动周期，默认为 Day
    /// </summary>
    /// <remarks>
    /// Minute, Hour, Day, Month, Year, Infinite
    /// </remarks>
    public static RollingInterval SerilogSinkToFileRollingInterval => Enum.TryParse<RollingInterval>(ReadEnvironmentVariable(
        "DODO_HOSTED_APP_LOGGER_SINK_TO_FILE_ROLLING_INTERVAL", "Day"), out var interval)
        ? interval
        : RollingInterval.Day;
    
    private static string ReadEnvironmentVariable(string key, string defaultValue) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))
            ? defaultValue
            : Environment.GetEnvironmentVariable(key)!;

    private static LogEventLevel ToLogEventLevel(this LogLevel level) => level switch
    {
        LogLevel.Trace => LogEventLevel.Verbose,
        LogLevel.Debug => LogEventLevel.Debug,
        LogLevel.Information => LogEventLevel.Information,
        LogLevel.Warning => LogEventLevel.Warning,
        LogLevel.Error => LogEventLevel.Error,
        LogLevel.Critical => LogEventLevel.Fatal,
        LogLevel.None => LogEventLevel.Information,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, null)
    };
}
