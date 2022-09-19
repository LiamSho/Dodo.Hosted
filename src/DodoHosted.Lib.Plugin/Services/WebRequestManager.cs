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

using System.Diagnostics;
using DodoHosted.Base.App.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace DodoHosted.Lib.Plugin.Services;

public class WebRequestManager : IWebRequestManager
{
    private readonly ILogger<WebRequestManager> _logger;
    private readonly IParameterResolver _parameterResolver;
    private readonly IServiceProvider _serviceProvider;
    private readonly IPluginManager _pluginManager;
    private readonly IChannelLogger _channelLogger;

    public WebRequestManager(
        ILogger<WebRequestManager> logger,
        IParameterResolver parameterResolver,
        IServiceProvider serviceProvider,
        IPluginManager pluginManager,
        IChannelLogger channelLogger)
    {
        _logger = logger;
        _parameterResolver = parameterResolver;
        _serviceProvider = serviceProvider;
        _pluginManager = pluginManager;
        _channelLogger = channelLogger;
    }
    
    public async Task<IActionResult> HandleRequestAsync(string identifier, string island, string name, HttpRequest body)
    {
        var islandId = island == "*" ? null : island;
        
        var plugin = _pluginManager.GetPlugin(identifier);
        if (plugin is null)
        {
            _logger.LogError("找不到 WebEvent 目标插件 {Identifier}", identifier);
            return new NotFoundObjectResult(new WebNotFoundResponse(WebNotFoundType.PluginNotFound));
        }

        var handler = plugin.Worker.WebHandlers.FirstOrDefault(x => x.Name == name);
        if (handler is null)
        {
            _logger.LogError("找不到 WebEvent 目标处理器 {Identifier} -> {Name}", identifier, name);
            return new NotFoundObjectResult(new WebNotFoundResponse(WebNotFoundType.WebRequestHandlerNotFound));
        }

        try
        {
            var sw = Stopwatch.StartNew();
            var scope = _serviceProvider.CreateScope();

            var parameters = _parameterResolver
                .GetHandlerConstructorInvokeParameter(handler.WebHandlerConstructor, plugin, scope.ServiceProvider);
            var ins = handler.WebHandlerConstructor.Invoke(parameters);
            var task = (Task<OkObjectResult>)handler.HandlerMethod.Invoke(ins, new object?[]{ body, islandId })!;
            var response = await task;
            
            scope.Dispose();
            sw.Stop();
            
            _logger.LogInformation("已处理 Web 事件: {WebEventName}, 插件：{WebEventPluginName}, 群组：{WebEventIsland}, 耗时: {WebEventProcessTime} MS",
                name, identifier, island, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            await _channelLogger.LogError(HostEnvs.DodoHostedAdminIsland,
                "事件处理器出现异常，" +
                $"Type：`{handler.WebHandlerType.FullName}`" +
                $"Exception：{ex.GetType().FullName}，" +
                $"Message：{ex.Message}");

            return new ObjectResult(new WebErrorResponse(ex))
            {
                StatusCode = StatusCodes.Status500InternalServerError
            };
        }
    }
}
