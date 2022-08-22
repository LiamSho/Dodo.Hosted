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
using DoDo.Open.Sdk.Models.Messages;
using DodoHosted.Base;
using DodoHosted.Base.App;
using DodoHosted.Base.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.Plugin;

public partial class PluginManager
{
    private static readonly string s_textMessage = typeof(DodoChannelMessageEvent<MessageBodyText>).FullName!;
    
    private void EventListener(IDodoHostedEvent dodoHostedEvent, string typeString)
    {
        if (typeString == s_textMessage)
        {
            RunCommand((dodoHostedEvent as DodoChannelMessageEvent<MessageBodyText>)!).GetAwaiter().GetResult();
        }

        var sw = Stopwatch.StartNew();
        var count = RunEvent(dodoHostedEvent, typeString).GetAwaiter().GetResult();
        sw.Stop();

        if (count == 0 && HostEnvs.DodoHostedLogEventWithoutHandler is false)
        {
            return;
        }
        
        _logger.LogInformation("已处理事件: {EventTypeString}, Handler 数量: {EventHandlerCount}, 耗时: {EventProcessTime} MS",
            typeString, count, sw.ElapsedMilliseconds);
    }
    
    /// <inheritdoc />
    public async Task<int> RunEvent(IDodoHostedEvent @event, string typeString, string? pluginIdentifier = null)
    {
        // 记录事件处理器数量
        var count = 0;
        
        // 插件中的事件处理器
        foreach (var (_, manifest) in _plugins)
        {
            // 检查 PluginIdentifier 是否匹配
            if (string.IsNullOrEmpty(pluginIdentifier) is false)
            {
                if (manifest.PluginInfo.Identifier != pluginIdentifier)
                {
                    continue;
                }
            }
            
            foreach (var eventHandler in manifest.EventHandlers)
            {
                if (eventHandler.EventTypeString != typeString)
                {
                    continue;
                }

                var scope = _provider.CreateScope();
                
                await (Task)eventHandler.HandlerMethod
                    .Invoke(eventHandler.EventHandler, new object?[]
                    {
                        @event, scope.ServiceProvider, _eventHandlerLogger
                    })!;
                
                scope.Dispose();
                count++;
            }
        }

        // 若 PluginIdentifier 不为空，或者不为 Native，则不处理 Native Assembly 中的事件处理器
        if (string.IsNullOrEmpty(pluginIdentifier) is false || pluginIdentifier != "native")
        {
            return count;
        }
        
        foreach (var nativeEventHandler in _nativeEventHandlers)
        {
            if (nativeEventHandler.EventTypeString != typeString)
            {
                continue;
            }
            
            var scope = _provider.CreateScope();
            
            await (Task)nativeEventHandler.HandlerMethod
                .Invoke(nativeEventHandler.EventHandler, new object?[]
                {
                    @event, scope.ServiceProvider, _eventHandlerLogger
                })!;
            
            scope.Dispose();
            count++;
        }

        return count;
    }
}
