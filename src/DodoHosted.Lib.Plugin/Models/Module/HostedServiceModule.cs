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

namespace DodoHosted.Lib.Plugin.Models.Module;

public class HostedServiceModule : IDisposable
{
    private readonly ILogger<HostedServiceModule> _logger;

    private readonly List<HostedService> _runningServices = new();

    public HostedServiceModule(
        IEnumerable<Type> types,
        IServiceProvider serviceProvider,
        IDynamicDependencyResolver dependencyResolver,
        ILogger<HostedServiceModule> logger)
    {
        _logger = logger;

        var hostedServiceTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(IPluginHostedService))
            .Where(x => x.IsAssignableTo(typeof(IPluginHostedService)))
            .ToList();

        foreach (var hostedServiceType in hostedServiceTypes)
        {
            var scope = serviceProvider.CreateScope();
            var cts = new CancellationTokenSource();
            var ins = dependencyResolver.GetDynamicObject<IPluginHostedService>(hostedServiceType, scope.ServiceProvider);
            var task = ins.StartAsync(cts.Token);
            
            _runningServices.Add(new HostedService(ins.HostedServiceName, cts, scope, task));
        }
    }

    public int Count()
    {
        return _runningServices.Count;
    }
    
    public void Dispose()
    {
        foreach (var service in _runningServices)
        {
            service.CancellationTokenSource.Cancel();
            try
            {
                service.Task.Wait();
            }
            catch (AggregateException ex)
            {
                if (ex.InnerExceptions.FirstOrDefault() is not TaskCanceledException)
                {
                    _logger.LogError(ex, "取消 Task {HostedServiceName} 出现异常", service.Name);
                }
            }
            
            service.Scope.Dispose();
        }
        
        GC.SuppressFinalize(this);
    }
    
    private record HostedService(string Name, CancellationTokenSource CancellationTokenSource, IServiceScope Scope, Task Task);
}
