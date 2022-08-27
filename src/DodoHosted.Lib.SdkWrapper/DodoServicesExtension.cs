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

using DoDo.Open.Sdk.Models;
using DoDo.Open.Sdk.Services;
using DodoHosted.Lib.SdkWrapper.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DodoHosted.Lib.SdkWrapper;

public static class DodoServicesExtension
{
    /// <summary>
    /// 注册 Dodo 机器人服务. 包含 Dodo 组件
    /// </summary>
    /// <param name="serviceCollection">Service Collection</param>
    /// <param name="dodoOpenApiOptionsBuilder">Dodo <see cref="OpenApiOptions"/> 构建器委托</param>
    /// <param name="dodoOpenEventOptionsBuilder">Dodo <see cref="OpenEventOptions"/> 构建器委托</param>
    /// <param name="includeEventProcessor">是否包含 <see cref="EventProcessService"/> 的实现，默认为 <c>true</c></param>
    /// <returns></returns>
    public static IServiceCollection AddDodoServices(
        this IServiceCollection serviceCollection,
        Action<DodoOpenApiOptionsBuilder> dodoOpenApiOptionsBuilder,
        Action<DodoOpenEventOptionsBuilder> dodoOpenEventOptionsBuilder,
        bool includeEventProcessor = true)
    {
        var openApiOptionsBuilder = new DodoOpenApiOptionsBuilder();
        var openEventOptionsBuilder = new DodoOpenEventOptionsBuilder();
        
        dodoOpenApiOptionsBuilder.Invoke(openApiOptionsBuilder);
        dodoOpenEventOptionsBuilder.Invoke(openEventOptionsBuilder);

        serviceCollection.AddSingleton(s =>
        {
            var loggerFactory = s.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<OpenApiService>();
            var options = openApiOptionsBuilder.UseLogger(logger).Build();
            return Options.Create(options);
        });
        serviceCollection.AddSingleton(_ =>
        {
            var options = openEventOptionsBuilder.Build();
            return Options.Create(options);
        });

        serviceCollection.AddSingleton(provider =>
        {
            var option = provider.GetRequiredService<IOptions<OpenApiOptions>>();
            return new OpenApiService(option.Value);
        });

        if (includeEventProcessor)
        {
            serviceCollection.AddSingleton<EventProcessService, DodoEventProcessor>();
        }
        
        serviceCollection.AddHostedService<DodoHosted>();

        return serviceCollection;
    }
}
