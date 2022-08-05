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

using DodoHosted.App;
using DodoHosted.Base;
using DodoHosted.Lib.Plugin;
using DodoHosted.Lib.SdkWrapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

Log.Logger = Helpers
    .GetLoggerConfiguration()
    .CreateLogger();

var builder = Host.CreateDefaultBuilder();

builder.ConfigureLogging(loggingBuilder =>
{
    loggingBuilder.ClearProviders();
});

builder.UseSerilog();

builder.ConfigureServices((_, services) =>
{
    services.AddDodoServices(
        openApiOptionsBuilder =>
        {
            openApiOptionsBuilder
                .UseBotId(AppEnvs.DodoBotClientId)
                .UseBotToken(AppEnvs.DodoBotToken)
                .UseLogger(AppEnvs.DodoHostedOpenApiLogLevel);
        },
        openEventOptionsBuilder =>
        {
            openEventOptionsBuilder
                .UseAsync()
                .UseReconnect();
        },
        AppDomain.CurrentDomain.GetAssemblies());

    services.AddPluginManager();
    services.AddBaseServices();
});

var app = builder.Build();

await app.RunAsync();
