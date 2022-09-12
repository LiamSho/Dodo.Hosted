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

using DodoHosted.Base;
using DodoHosted.Base.App;
using DodoHosted.Base.Command;
using DodoHosted.Base.Types;
using DodoHosted.Lib.Plugin.Exceptions;
using DodoHosted.Lib.Plugin.Models;
using DodoHosted.Open.Plugin;

namespace DodoHosted.Lib.Plugin.Helper;

public static class CommandMethodInvoker
{
    public static async Task<CommandExecutionResult> Invoke(this CommandManifest cmdManifest, CommandParsed commandParsed, PluginBase.Context context)
    {
        var node = cmdManifest.RootNode;
        // ReSharper disable once LoopCanBeConvertedToQuery
        foreach (var path in commandParsed.Path)
        {
            node = node?.GetChild(path);
        }
        
        if (node?.Method is null)
        {
            await context.Functions.Reply.Invoke($"找不到指令，请输入 {HostEnvs.CommandPrefix}help -n {cmdManifest.RootNode.Value} 查看帮助");
            return CommandExecutionResult.Unknown;
        }

        if (string.IsNullOrEmpty(node.PermissionNode) is false)
        {
            if (await context.Functions.PermissionCheck.Invoke(node.PermissionNode) is false)
            {
                await context.Functions.Reply.Invoke("你没有权限执行此指令");
                return CommandExecutionResult.Unauthorized;
            }
        }

        var result = await node.Invoke(commandParsed, context, cmdManifest.CommandExecutor);

        return result;
    }
    
    private static async Task<CommandExecutionResult> Invoke(this CommandNode node, CommandParsed commandParsed, PluginBase.Context context, ICommandExecutor obj)
    {
        var paramLength = node.Options!.Count + (node.ContextParamOrder == -1 ? 0 : 1);
        var parameters = new object?[paramLength];

        if (node.ContextParamOrder != -1)
        {
            parameters[(int)node.ContextParamOrder!] = context;
        }
        
        var methodParameters = node.Options;
        
        foreach (var (order, (type, attr)) in methodParameters)
        {
            var keys = attr.Abbr is null ? new[] { $"-{attr.Name}" } : new[] { $"-{attr.Name}", attr.Abbr };
            var hasValue = commandParsed.Arguments.TryGetValueByMultipleKey(keys, out var value);
            if (hasValue is false)
            {
                if (attr.Required)
                {
                    if (type == typeof(bool))
                    {
                        parameters[order] = false;
                    }
                    
                    await context.Functions.Reply.Invoke($"缺少参数 {attr.Name}");
                    return CommandExecutionResult.Failed;
                }

                parameters[order] = null;
            }
            else
            {
                if (CommandTypeHelper.SupportedBasicValueTypes.Contains(type))
                {
                    var nonNullableType = Nullable.GetUnderlyingType(type) ?? type;
                    
                    var converted = Convert.ChangeType(value, nonNullableType);
                    if (converted is null)
                    {
                        await context.Functions.Reply.Invoke($"参数类型错误，{attr.Name} 需要 {nonNullableType.Name} 类型参数");
                        return CommandExecutionResult.Failed;
                    }
                    parameters[order] = converted;
                    continue;
                }

                if (type == typeof(DodoChannelId))
                {
                    var converted = new DodoChannelId(value!);
                    parameters[order] = converted;
                    continue;
                }
                
                if (type == typeof(DodoChannelIdWithWildcard))
                {
                    var converted = new DodoChannelIdWithWildcard(value!);
                    parameters[order] = converted;
                    continue;
                }
                
                if (type == typeof(DodoMemberId))
                {
                    var converted = new DodoMemberId(value!);
                    parameters[order] = converted;
                    continue;
                }

                // ReSharper disable once InvertIf
                if (type == typeof(DodoEmoji))
                {
                    var converted = new DodoEmoji(value!);
                    parameters[order] = converted;
                    continue;
                }

                throw new InternalProcessException(nameof(CommandMethodInvoker), nameof(Invoke), $"未知的参数类型: {type.FullName}");
            }
        }

        var task = (Task<bool>)node.Method!.Invoke(obj, parameters)!;
        
        var result = await task;
        
        return result ? CommandExecutionResult.Success : CommandExecutionResult.Failed;
    }

    internal static bool TryGetValueByMultipleKey<T, K>(this IReadOnlyDictionary<T, K> dictionary, IEnumerable<T> keys, out K? value) where T : notnull
    {
        foreach (var key in keys)
        {
            if (dictionary.TryGetValue(key, out value))
            {
                return true;
            }
        }

        value = default;
        return false;
    }
}
