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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.App.Core;

public static class DodoHostedExtension
{
    /// <summary>
    /// 添加所有 DodoHosted 所需服务
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <param name="openApiOptionsBuilder">DodoOpenApi 选项构建器</param>
    /// <param name="openEventOptionsBuilder">DodoOpenEvent 选项构建器</param>
    /// <returns></returns>
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

    /// <summary>
    /// 添加 Web 相关服务，包含跨域，控制器，<see cref="HttpContextAccessor"/>，以及开启 Proxy 后的 ForwardedHeaders 配置
    /// </summary>
    /// <param name="services">DI 容器</param>
    /// <returns></returns>
    public static IServiceCollection AddDodoHostedWebServices(this IServiceCollection services)
    {
        services.AddBaseWebServices();
        
        return services;
    }

    /// <summary>
    /// 配置 Web 中间件，依次开启跨域，路由，映射控制器，开启 Proxy 后将会加入 ForwardedHeaders 配置
    /// </summary>
    /// <param name="app">Application Builder</param>
    /// <param name="beforeRoutingConfiguration">自定义配置在 Routing 前，Cors 后的中间件</param>
    /// <returns></returns>
    public static IApplicationBuilder UseDodoHostedWebPipeline(this IApplicationBuilder app, Action<IApplicationBuilder>? beforeRoutingConfiguration = null)
    {
        app.UseBaseWebPipeline(beforeRoutingConfiguration);
        
        return app;
    }
}
