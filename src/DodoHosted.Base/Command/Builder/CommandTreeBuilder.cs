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
using DodoHosted.Base.Types;

namespace DodoHosted.Base.Command.Builder;

public class CommandTreeBuilder
{
    private readonly string _nodeName;
    private readonly string _description;
    private readonly string _permission;
    private readonly MethodInfo? _method;

    private readonly bool _isRoot;
    
    private readonly List<CommandTreeBuilder> _children = new();

    public CommandTreeBuilder(string nodeName, string description, string? permNode = null, Delegate? method = null)
    {
        if (permNode == "*")
        {
            throw new CommandNodeException("权限节点不可为通配符");
        }
        
        _nodeName = nodeName;
        _description = description;
        _permission = permNode ?? nodeName;
        _method = method?.Method;

        _isRoot = true;
    }

    private CommandTreeBuilder(string nodeName, string description, string? permNode = null, Delegate? method = null, bool isRoot = false)
        : this(nodeName, description, permNode, method)
    {
        _isRoot = isRoot;
    }

    public CommandTreeBuilder Then(string nodeName, string description, string? permNode = null, Delegate? method = null, Action<CommandTreeBuilder>? builder = null)
    {
        if (_children.Any(x => x._nodeName == nodeName))
        {
            throw new CommandNodeException("重复的子指令节点");
        }

        var newNode = new CommandTreeBuilder(nodeName, description, permNode, method, false);
        
        builder?.Invoke(newNode);
        _children.Add(newNode);
        return this;
    }

    public CommandNode Build()
    {
        BuildMethodMetadata(_method, out var contextParamOrder, out var paramOptions);

        var current = new CommandNode(_nodeName, _description, _method, _permission, contextParamOrder, paramOptions);

        var children = _children.Select(x => x.Build()).ToArray();
        
        foreach (var child in children)
        {
            var commandNode = child;
            current.AddChild(ref commandNode);
        }

        if (_isRoot is false)
        {
            return current;
        }

        if (current.PermissionNode == string.Empty)
        {
            throw new CommandNodeException("根指令节点的权限节点名不可为空字符串");
        }

        return current;
    }

    private static void BuildMethodMetadata(MethodInfo? methodInfo, out int? contextParamOrder, out Dictionary<int, (Type, CmdOptionAttribute)>? paramOptions)
    {
        if (methodInfo is null)
        {
            contextParamOrder = null;
            paramOptions = null;
            
            return;
        }
        
        var parameters = methodInfo.GetParameters();
        
        // 除了 PluginBase.Context 之外的参数，均需要带有 CmdOptionAttribute
        if (parameters
            .SkipWhile(x => x.ParameterType == typeof(PluginBase.Context))
            .Select(p => p.GetCustomAttribute<CmdOptionAttribute>())
            .Any(cmdOption => cmdOption is null))
        {
            throw new CommandNodeException(methodInfo, "参数必须标记 CmdOptionAttribute");
        }

        // 参数列表
        paramOptions = parameters
            .Where(x => x.GetCustomAttribute<CmdOptionAttribute>() is not null)
            .Select(x => (x.Position, (x.ParameterType, x.GetCustomAttribute<CmdOptionAttribute>()!)))
            .ToDictionary(x => x.Position, y => y.Item2);
        
        // 检查参数名重复和保留字
        var paramNames = paramOptions
            .Select(x => x.Value.Item2.Name)
            .ToArray();
        if (paramNames.Length != paramNames.Distinct().Count())
        {
            throw new CommandNodeException(methodInfo, "存在参数名重复");
        }
        if (paramNames.Any(x => x == "help"))
        {
            throw new CommandNodeException(methodInfo, "存在名为 help 的保留参数名");
        }
        var paramAbbr = paramOptions
            .Select(x => x.Value.Item2.Abbr)
            .SkipWhile(x => x is null)
            .ToArray();
        if (paramAbbr.Length != paramAbbr.Distinct().Count())
        {
            throw new CommandNodeException(methodInfo, "存在参数名简称重复");
        }
        if (paramAbbr.Any(x => x == "?"))
        {
            throw new CommandNodeException(methodInfo, "存在名为 ? 的保留参数名简称");
        }

        // 检查参数类型
        var unsupported = new List<string>();
        foreach (var (_, (type, attribute)) in paramOptions)
        {
            if (CommandTypeHelper.SupportedCmdOptionTypes.Contains(type) is false)
            {
                unsupported.Add($"参数 {attribute.Name} 的类型 {type.FullName} 不受支持");
                continue;
            }

            // ReSharper disable once InvertIf
            if (attribute.Required is false)
            {
                if (CommandTypeHelper.SupportedCmdOptionNullableTypes.Contains(type) is false)
                {
                    unsupported.Add($"可选参数 {attribute.Name} 的类型必须为 Nullable 类型");
                }
            }
        }
        
        if (unsupported.Count != 0)
        {
            throw new CommandNodeException(methodInfo, $"包含错误的参数: {string.Join(", ", unsupported)}");
        }
                
        var contextParam = methodInfo.GetParameters()
            .FirstOrDefault(x => x.ParameterType == typeof(PluginBase.Context));
        contextParamOrder = contextParam?.Position ?? -1;

        var totalParams = methodInfo.GetParameters().Length;
        var calculatedParams = paramOptions.Count + (contextParamOrder == -1 ? 0 : 1);
                
        if (totalParams != calculatedParams)
        {
            throw new CommandNodeException(methodInfo, "存在不可知参数");
        }
    }
}
