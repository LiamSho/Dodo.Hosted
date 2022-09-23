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

using System.Reflection;
using DodoHosted.Base.App.Context;
using DodoHosted.Base.App.Exceptions;
using DodoHosted.Base.App.Interfaces;

namespace DodoHosted.Base.App.Command;

public class CommandNode
{
    public string Value { get; }
    public CommandNode? Parent { get; private set; }
    public int Depth { get; private set; }
    public string Description { get; init; }
    public MethodInfo? Method { get; init; }
    public int? ContextParamOrder { get; init; }
    public Dictionary<int, Type> ServiceOptions { get; init; }
    public Dictionary<int, (Type, CmdOptionAttribute)> Options { get; init; }
    public bool AdminIslandOnly { get; init; }

    private readonly IDynamicDependencyResolver _dependencyResolver;

    public int MaxDepth
    {
        get
        {
            RequireRoot();

            return RMaxDepth;
        }
    }
    public int NodeCount
    {
        get
        {
            RequireRoot();

            return RNodeCount;
        }
    }

    public string PermissionNode => RPermissionNode;

    private readonly List<CommandNode> _children = new();
    private readonly string? _permissionNode;

    private int RMaxDepth => _children.Count == 0 ? Depth : _children.Max(x => x.RMaxDepth);
    private int RNodeCount => _children.Count == 0 ? 1 : _children.Sum(x => x.RNodeCount) + 1;

    private string RPermissionNode
    {
        get
        {
            var node = this;
            var perm = _permissionNode ?? Value;

            while (node.Parent is not null)
            {
                // 如果是 NULL，取指令名称
                var prev = node.Parent._permissionNode switch
                {
                    null => node.Parent.Value,
                    _ => node.Parent._permissionNode
                };

                // 如果前一个为空字符串，不管
                
                // 如果前一个不是空字符串
                if (prev != string.Empty)
                {
                    // 如果当前不是空字符串，加个点
                    // 如果当前是空字符串，取前一个的值
                    perm = perm != string.Empty ? $"{prev}.{perm}" : prev;
                }
                
                // 下一个
                node = node.Parent;
            }

            return perm;
        }
    }

    public CommandNode(
        string value, string description,
        IDynamicDependencyResolver dependencyResolver,
        bool adminIslandOnly = false,
        MethodInfo? method = null,
        string? permNode = null,
        int? contentParamOrder = null,
        Dictionary<int, Type>? serviceOptions = null,
        Dictionary<int, (Type, CmdOptionAttribute)>? options = null)
    {
        _dependencyResolver = dependencyResolver;
        
        AdminIslandOnly = adminIslandOnly;
        
        Value = value;
        Description = description;
        Parent = null;
        Depth = 0;
        
        Method = method;
        ContextParamOrder = contentParamOrder;
        ServiceOptions = serviceOptions ?? new Dictionary<int, Type>();
        Options = options ?? new Dictionary<int, (Type, CmdOptionAttribute)>();
        
        _permissionNode = permNode;
    }
    
    public void AddChild(ref CommandNode node)
    {
        node.Parent = this;
        _children.Add(node);
        UpdateChildren();
    }
    
    public void RemoveChild(ref CommandNode node)
    { 
        _children.Remove(node);
    }

    public CommandNode? GetChild(string value)
    {
        return _children.FirstOrDefault(x => x.Value == value);
    }
    public IEnumerable<CommandNode> GetChildren()
    {
        return _children;
    }
    
    public IEnumerable<CommandNode> GetNodes(int depth)
    {
        RequireRoot();

        if (depth <= 0)
        {
            throw new CommandNodeException("Depth 不可小于或等于 0");
        }

        IEnumerable<CommandNode> nodes = _children;
        for (var i = 1; i < depth; i++)
        {
            nodes = nodes.SelectMany(x => x._children);
        }

        return nodes;
    }
    public IEnumerable<CommandNode> GetNodes(params string[] path)
    {
        RequireRoot();
        
        var node = this;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var p in path)
        {
            node = node?._children.FirstOrDefault(x => x.Value == p);
        }

        return node?._children ?? Enumerable.Empty<CommandNode>();
    }
    
    public CommandNode? GetNode(params string[] path)
    {
        RequireRoot();
        
        var node = this;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var p in path)
        {
            node = node?._children.FirstOrDefault(x => x.Value == p);
        }

        return node;
    }

    public async Task<CommandExecutionResult> Invoke(object obj, CommandContext context, IServiceProvider serviceProvider)
    {
        var allowed = await context.PermissionCheck(PermissionNode);
        if (allowed is false)
        {
            return CommandExecutionResult.Unauthorized;
        }
        
        if (Method is null)
        {
            var fullPath = context.CommandParsed.CommandName + " " + string.Join(" ", context.CommandParsed.Path);
            await context.Reply.Invoke($"未知的指令路径，请输入 `{HostEnvs.CommandPrefix}{fullPath} -?` 查看帮助");
            return CommandExecutionResult.Unknown;
        }
        
        var parameterInfos = Method.GetParameters();
        var parameters = new object?[parameterInfos.Length];
        _dependencyResolver.SetInjectableParameterValues(parameterInfos, serviceProvider, ref parameters);
        _dependencyResolver.SetCommandOptionParameterValues(this, context.CommandParsed, ref parameters);

        if (ContextParamOrder is not null)
        {
            parameters[ContextParamOrder.Value] = context;
        }

        var result = await (Task<bool>)Method.Invoke(obj, parameters)!;

        return result ? CommandExecutionResult.Success : CommandExecutionResult.Failed;
    }
    
    private void UpdateChildren()
    {
        Depth = Parent?.Depth + 1 ?? 0;

        foreach (var child in _children)
        {
            child.UpdateChildren();
        }
    }

    private void RequireRoot()
    {
        if (Parent is not null)
        {
            throw new CommandNodeException("此方法只能在根节点上调用");
        }
    }
}
