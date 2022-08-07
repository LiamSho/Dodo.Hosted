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

using DodoHosted.Base.App;
using DodoHosted.Lib.Plugin;
using DodoHosted.Lib.SdkWrapper;
using DodoHosted.Lib.SdkWrapper.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.App.Core;

public static class DodoHostedServiceExtension
{
    public static IServiceCollection AddDodoHostedServices(this IServiceCollection services,
        Action<DodoOpenApiOptionsBuilder>? openApiOptionsBuilder = null,
        Action<DodoOpenEventOptionsBuilder>? openEventOptionsBuilder = null)
    {
        var apiOptionsBuild = openApiOptionsBuilder ??(b => b
                .UseBaseApi(HostEnvs.DodoSdkApiEndpoint)
                .UseBotId(HostEnvs.DodoSdkBotClientId)
                .UseBotToken(HostEnvs.DodoSdkBotToken)
                .UseLogger(HostEnvs.DodoHostedOpenApiLogLevel));

        var eventOptionsBuilder = openEventOptionsBuilder ?? (b => b
            .UseAsync()
            .UseReconnect());
        
        services.AddDodoServices(apiOptionsBuild, eventOptionsBuilder);
        services.AddPluginManager();
        services.AddBaseServices();

        return services;
    }
}
