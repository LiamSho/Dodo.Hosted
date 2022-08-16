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

using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Models;
using DodoHosted.Open.Plugin;

namespace BasicListenerPlugin;

public class PluginLogSwitchCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(string[] args, CommandMessage message, IServiceProvider provider, IPermissionManager permissionManager,
        Func<string, Task<string>> reply, bool shouldAllow = false)
    {
        if (shouldAllow is false)
        {
            if (await permissionManager.CheckPermission("blp", message) is false)
            {
                return CommandExecutionResult.Unauthorized;
            }
        }

        var arg = args.Skip(1).FirstOrDefault();
        if (arg is null)
        {
            return CommandExecutionResult.Unknown;
        }

        switch (arg)
        {
            case "switch":
                TextMessageListener.IsEnabled = !TextMessageListener.IsEnabled;
                await reply.Invoke($"当前状态：{TextMessageListener.IsEnabled}");
                return CommandExecutionResult.Success;
            case "status":
                await reply.Invoke($"当前状态：{TextMessageListener.IsEnabled}");
                return CommandExecutionResult.Success;
            default:
                return CommandExecutionResult.Unknown;
        }
    }

    public CommandMetadata GetMetadata() => new(
        CommandName: "blp",
        Description: "用于开关频道文字信息日志记录",
        HelpText: @"""
- `{{PREFIX}}blp switch`    切换状态
- `{{PREFIX}}blp status`    查看当前状态
""",
        PermissionNodes: new Dictionary<string, string> { { "blp", "允许使用 `blp` 指令" } });
}
