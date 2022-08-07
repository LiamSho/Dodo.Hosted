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

using System.Runtime.InteropServices;
using System.Text;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Models;
using DodoHosted.Open.Plugin;

namespace DodoHosted.Lib.Plugin.Builtin;

[CommandExecutor(
    commandName: "system",
    description: "获取系统信息",
    helpText: @"""
- {{PREFIX}}system gc    查看系统 GC 信息
- {{PREFIX}}system info    查看系统信息
""")]
public class SystemCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage message,
        IServiceProvider provider,
        Func<string, Task<string>> reply,
        bool shouldAllow = false)
    {
        if (shouldAllow is false)
        {
            return CommandExecutionResult.Unauthorized;
        }

        if (args.Length > 2)
        {
            return CommandExecutionResult.Unknown;
        }

        var arg = args.Skip(1).FirstOrDefault();
        var messageBuilder = new StringBuilder();
        switch (arg)
        {
            case "gc":
                var gcInfo = GC.GetGCMemoryInfo();
                messageBuilder.AppendLine($"Max Generation: `{GC.MaxGeneration}`");
                messageBuilder.AppendLine($"Finalization Pending Count: `{gcInfo.FinalizationPendingCount}`");
                messageBuilder.AppendLine($"Total Heap Size: `{ToMegabytes(gcInfo.HeapSizeBytes)} MB`");
                messageBuilder.AppendLine($"Committed Heap Size: `{ToMegabytes(gcInfo.TotalCommittedBytes)} MB`");
                messageBuilder.AppendLine($"Total Allocated Memory: `{ToMegabytes(GC.GetTotalMemory(false))} MB`");
                break;
            case "info":
                messageBuilder.AppendLine($"System Time: `{DateTimeOffset.UtcNow.AddHours(8).ToString("u")}`");
                messageBuilder.AppendLine($"DodoHosted Version: `{HostEnvs.DodoHostedVersion}`");
                messageBuilder.AppendLine($"Containerized: `{HostEnvs.DodoHostedInContainer.ToString()}`");
                messageBuilder.AppendLine($".NET Runtime Framework: `{RuntimeInformation.FrameworkDescription}`");
                messageBuilder.AppendLine($".NET Runtime Identifier: `{RuntimeInformation.RuntimeIdentifier}`");
                break;
            default:
                return CommandExecutionResult.Unknown;
        }

        await reply.Invoke(messageBuilder.ToString());

        return CommandExecutionResult.Success;
    }

    private static string ToMegabytes(long size)
    {
        return ((double)size / 1024 / 1024).ToString("F");
    }
}
