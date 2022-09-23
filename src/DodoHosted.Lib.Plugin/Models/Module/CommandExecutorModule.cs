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

using DodoHosted.Base.App.Context;

namespace DodoHosted.Lib.Plugin.Models.Module;

public class CommandExecutorModule : IDisposable
{
    private readonly Dictionary<string, (ICommandExecutor, CommandNode)> _rootNodes = new();
    
    public CommandExecutorModule(IEnumerable<Type> types, IDynamicDependencyResolver dependencyResolver)
    {
        var commandExecutorTypes = types
            .Where(x => x.IsSealed)
            .Where(x => x != typeof(ICommandExecutor))
            .Where(x => x.IsAssignableTo(typeof(ICommandExecutor)))
            .Where(x => x.ContainsGenericParameters is false);

        foreach (var type in commandExecutorTypes)
        {
            var instance = Activator.CreateInstance(type);
            
            if (instance is not ICommandExecutor executor)
            {
                throw new PluginAssemblyLoadException($"无法创建指令处理器 {type.FullName} 的实例");
            }

            var node = executor.GetBuilder().Build(dependencyResolver);

            _rootNodes.Add(node.Value, (executor, node));
        }
    }

    public IEnumerable<CommandNode> GetCommandNodes()
    {
        return _rootNodes.Select(x => x.Value.Item2);
    }

    public CommandNode? GetCommandNode(string name)
    {
        return _rootNodes.ContainsKey(name) is false
            ? null
            : _rootNodes.FirstOrDefault(x => x.Value.Item2.Value == name).Value.Item2;
    }

    public async Task<CommandExecutionResult> Invoke(CommandContext context, IServiceProvider serviceProvider)
    {
        var name = context.CommandParsed.CommandName;

        if (_rootNodes.ContainsKey(name) is false)
        {
            throw new Exception();
        }
        
        var executor = _rootNodes
            .FirstOrDefault(x => x.Key == name);

        var (ins, root) = executor.Value;

        var node = root.GetNode(context.CommandParsed.Path);

        if (node is not null)
        {
            return await node.Invoke(ins, context, serviceProvider);
        }

        var fullPath = context.CommandParsed.CommandName + " " + string.Join(" ", context.CommandParsed.Path);
        await context.Reply.Invoke($"未知的指令路径，请输入 `{HostEnvs.CommandPrefix}{fullPath} -?` 查看帮助");
        return CommandExecutionResult.Unknown;
    }

    public int Count()
    {
        return _rootNodes.Count;
    }
    
    public void Dispose()
    {
        _rootNodes.Clear();
    }
}
