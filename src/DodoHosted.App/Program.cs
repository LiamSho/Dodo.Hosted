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
using DodoHosted.App.Core;
using DodoHosted.Lib.Plugin;
using Serilog;

Log.Logger = Helpers
    .GetLoggerConfiguration()
    .CreateLogger();

PluginManager.NativeAssemblies.AddRange(new[] { typeof(LocalTestPlugin.Entry).Assembly });

var builder = WebApplication.CreateBuilder();

builder.Logging.ClearProviders();
builder.Host.UseSerilog();

builder.Services.AddDodoHostedServices();
builder.Services.AddDodoHostedWebServices();

var app = builder.Build();

app.UseDodoHostedWebPipeline();

await app.RunAsync();
