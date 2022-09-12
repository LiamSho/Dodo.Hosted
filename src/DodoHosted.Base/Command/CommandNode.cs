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
using DodoHosted.Base.Command.Attributes;
using DodoHosted.Base.Exceptions;

namespace DodoHosted.Base.Command;

public class CommandNode
{
    public string Value { get; }
    public CommandNode? Parent { get; private set; }
    public int Depth { get; private set; }
    public string Description { get; init; }
    public MethodInfo? Method { get; init; }
    public int? ContextParamOrder { get; init; }
    public Dictionary<int, (Type, CmdOptionAttribute)> Options { get; init; }

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
        MethodInfo? method = null,
        string? permNode = null,
        int? contentParamOrder = null,
        Dictionary<int, (Type, CmdOptionAttribute)>? options = null)
    {
        Value = value;
        Description = description;
        Parent = null;
        Depth = 0;
        
        Method = method;
        ContextParamOrder = contentParamOrder;
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