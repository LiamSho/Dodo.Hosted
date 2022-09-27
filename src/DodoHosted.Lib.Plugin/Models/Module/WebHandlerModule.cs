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

using DodoHosted.Base.App.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
// ReSharper disable SuggestBaseTypeForParameterInConstructor

namespace DodoHosted.Lib.Plugin.Models.Module;

public class WebHandlerModule : IDisposable
{
    private readonly Dictionary<string, Func<HttpRequest, Task<OkObjectResult>>> _handlers = new();

    public WebHandlerModule(
        IEnumerable<Type> types,
        IServiceProvider serviceProvider,
        IDynamicDependencyResolver dependencyResolver,
        ILogger<WebHandlerModule> logger)
    {
        var webHandlerTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(IPluginWebHandler))
            .Where(x => x.IsAssignableTo(typeof(IPluginWebHandler)));

        foreach (var webHandlerType in webHandlerTypes)
        {
            var attr = webHandlerType.GetCustomAttribute<NameAttribute>();
            var name = attr?.Name ?? webHandlerType.Name;

            if (attr is null)
            {
                logger.LogWarning("{WebHandlerTypeName} 缺少 NameAttribute，默认使用类型名作为名称", webHandlerType.FullName);
            }
            
            _handlers.Add(
                name,
                async r =>
                {
                    var scope = serviceProvider.CreateScope();
                    var ins = dependencyResolver.GetDynamicObject<IPluginWebHandler>(webHandlerType, scope.ServiceProvider);

                    var response = await ins.Handle(r);
                    
                    scope.Dispose();

                    return response;
                });
            
            logger.LogInformation("已载入 Web 事件处理器 {LoadedWebHandlerName} -> {LoadedWebHandlerType}", name, webHandlerType.FullName);
        }
    }

    public async Task<IActionResult> Invoke(string name, HttpRequest request)
    {
        if (_handlers.ContainsKey(name) is false)
        {
            return new NotFoundObjectResult(new WebNotFoundResponse(WebNotFoundType.WebRequestHandlerNotFound));
        }

        var response = await _handlers[name].Invoke(request);
        return response;
    }

    public int Count()
    {
        return _handlers.Count;
    }
    
    public void Dispose()
    {
        _handlers.Clear();
    
        GC.SuppressFinalize(this);
    }
}
