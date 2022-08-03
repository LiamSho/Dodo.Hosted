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
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DodoHosted.Lib.SdkWrapper;

public class DodoHosted : IHostedService
{
    private readonly IHostApplicationLifetime _applicationLifetime;
    private readonly OpenEventService _openEventService;

    private Task? _dodoEventReceiverTask;
    
    public DodoHosted(
        IHostApplicationLifetime applicationLifetime,
        EventProcessService eventProcessor,
        OpenApiService openApiService,
        IOptions<OpenEventOptions> openEventOptions)
    {
        _applicationLifetime = applicationLifetime;
        _openEventService = new OpenEventService(openApiService, eventProcessor, openEventOptions.Value);
        _dodoEventReceiverTask = null;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _applicationLifetime.ApplicationStarted.Register(StartListener);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _openEventService.StopReceiveAsync().GetAwaiter().GetResult();
        _dodoEventReceiverTask?.GetAwaiter().GetResult();

        return Task.CompletedTask;
    }

    private void StartListener()
    {
        _dodoEventReceiverTask = _openEventService.ReceiveAsync();
    }
}
