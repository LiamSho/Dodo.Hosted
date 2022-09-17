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

namespace DodoHosted.Lib.Plugin.Helper;

public static class CommandMethodInvoker
{
    public static async Task<CommandExecutionResult> Invoke(
        this CommandManifest cmdManifest,
        PluginManifest pluginManifest,
        CommandParsed commandParsed,
        PluginBase.Context context)
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

        var commandParameterHelper = context.Provider.GetRequiredService<ICommandParameterResolver>();
        var parameters = commandParameterHelper.GetMethodInvokeParameter(node, pluginManifest, commandParsed, context);
        if (node.ContextParamOrder != -1)
        {
            parameters[(int)node.ContextParamOrder!] = context;
        }

        var task = (Task<bool>)node.Method!.Invoke(cmdManifest.CommandExecutor, parameters)!;
        
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
