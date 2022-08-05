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

using Serilog;
using Serilog.Events;

namespace DodoHosted.App;

public static class Helpers
{
    public static LoggerConfiguration GetLoggerConfiguration()
    {
        var loggerConfiguration = new LoggerConfiguration();

        const string LoggerTemplate = "[{Timestamp:HH:mm:ss} {Level:u3}] <{ThreadId} {ThreadName}> {Message:lj}{NewLine}{Exception}";
        
        loggerConfiguration.MinimumLevel.Is(AppEnvs.SerilogMinimumLevel);

        loggerConfiguration.MinimumLevel.Override("Microsoft", LogEventLevel.Warning);
        loggerConfiguration.MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information);

        loggerConfiguration.Enrich.FromLogContext();
        loggerConfiguration.Enrich.WithThreadId();
        loggerConfiguration.Enrich.WithThreadName();
        
        loggerConfiguration.WriteTo.Console(outputTemplate: LoggerTemplate);

        if (string.IsNullOrEmpty(AppEnvs.SerilogSinkToFile) is false)
        {
            loggerConfiguration
                .WriteTo.File(
                    outputTemplate: LoggerTemplate,
                    path: AppEnvs.SerilogSinkToFile,
                    rollingInterval: AppEnvs.SerilogSinkToFileRollingInterval);
        }
        
        return loggerConfiguration;
    }
}
