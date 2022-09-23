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
using DodoHosted.Base.Events;
using DodoHosted.Lib.Plugin.Models.Module;
using DodoHosted.Lib.SdkWrapper;

namespace DodoHosted.Lib.Plugin.Services;

public class EventManager : IEventManager
{
    private readonly ILogger<EventManager> _logger;
    private readonly IServiceProvider _provider;
    private readonly IChannelLogger _channelLogger;
    private readonly ICommandManager _commandManager;
    private readonly IPluginManager _pluginManager;

    public EventManager(
        ILogger<EventManager> logger,
        IServiceProvider provider,
        IChannelLogger channelLogger,
        ICommandManager commandManager,
        IPluginManager pluginManager)
    {
        _logger = logger;
        _provider = provider;
        _channelLogger = channelLogger;
        _commandManager = commandManager;
        _pluginManager = pluginManager;
    }

    /// <inheritdoc />
    public async Task<int> RunEvent(IDodoHostedEvent @event, string typeString, string? pluginIdentifier = null)
    {
        // 记录事件处理器数量
        var count = 0;
        
        // 若 PluginIdentifier 为 null，运行所有事件处理器
        // 若 PluginIdentifier 为 其他值，运行指定插件的事件处理器
        var eventHandlerModules = pluginIdentifier switch
        {
            null => _pluginManager.GetEventHandlerModules(),
            _ => _pluginManager.GetEventHandlerModule(pluginIdentifier) is null
                ? Enumerable.Empty<EventHandlerModule>()
                : new [] { _pluginManager.GetEventHandlerModule(pluginIdentifier)! }
        };

        foreach (var ehm in eventHandlerModules)
        {
            try
            {
                var scope = _provider.CreateScope();
                count += await ehm.Invoke(typeString, @event);
                scope.Dispose();
            }
            catch (EventHandlerExecutionException e)
            {
                foreach (var (ts, ex) in e.ExecutionExceptions)
                {
                    await _channelLogger.LogError(HostEnvs.DodoHostedAdminIsland,
                        "事件处理器出现异常，" +
                        $"Type：`{ts}`" +
                        $"Exception：{ex.GetType().FullName}，" +
                        $"Message：{ex.Message}");
                }
            }
            catch (Exception e)
            {
                await _channelLogger.LogError(HostEnvs.DodoHostedAdminIsland,
                    "事件处理器出现异常，" +
                    $"Exception：{e.GetType().FullName}，" +
                    $"Message：{e.Message}，" +
                    $"\n{e.StackTrace}");
            }
            count++;
        }

        return count;
    }

    /// <inheritdoc />
    public void Initialize()
    {
        DodoEventProcessor.DodoEvent += EventListener;
    }
    
    private static readonly string s_textMessage = typeof(DodoChannelMessageEvent<MessageBodyText>).FullName!;

    private void EventListener(IDodoHostedEvent dodoHostedEvent, string typeString)
    {
        if (typeString == s_textMessage)
        {
            _commandManager.RunCommand((dodoHostedEvent as DodoChannelMessageEvent<MessageBodyText>)!).GetAwaiter().GetResult();
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
}
