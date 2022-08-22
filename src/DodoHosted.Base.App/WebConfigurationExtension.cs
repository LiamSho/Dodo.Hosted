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

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Base.App;

public static class WebConfigurationExtension
{
    /// <summary>
    /// 添加 Web 相关服务，包含跨域，控制器，<see cref="HttpContextAccessor"/>，以及开启 Proxy 后的 ForwardedHeaders 配置
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <returns></returns>
    public static IServiceCollection AddBaseWebServices(this IServiceCollection serviceCollection)
    {
        serviceCollection.AddCors();
        serviceCollection.AddControllers();
        serviceCollection.AddHttpContextAccessor();

        if (HostEnvs.DodoHostedWebBehindProxy)
        {
            serviceCollection.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.All;
            });
        }
        
        return serviceCollection;
    }

    /// <summary>
    /// 配置 Web 中间件，依次开启跨域，路由，映射控制器，开启 Proxy 后将会加入 ForwardedHeaders 配置
    /// </summary>
    /// <param name="applicationBuilder">Application Builder</param>
    /// <param name="beforeRoutingConfiguration">自定义配置在 Routing 前，Cors 后的中间件</param>
    /// <returns></returns>
    public static IApplicationBuilder UseBaseWebPipeline(this IApplicationBuilder applicationBuilder, Action<IApplicationBuilder>? beforeRoutingConfiguration = null)
    {
        if (HostEnvs.DodoHostedWebBehindProxy)
        {
            applicationBuilder.UseForwardedHeaders();
        }
        
        applicationBuilder.UseCors(options =>
        {
            options.SetIsOriginAllowed(_ => true)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });

        beforeRoutingConfiguration?.Invoke(applicationBuilder);
        
        applicationBuilder.UseRouting();
        
        applicationBuilder.UseEndpoints(builder =>
        {
            builder.MapControllers();
        });
        
        return applicationBuilder;
    }
}
