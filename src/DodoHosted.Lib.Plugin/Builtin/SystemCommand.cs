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
using DoDo.Open.Sdk.Models.Islands;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Lib.Plugin.Builtin;

public class SystemCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage message,
        IServiceProvider provider,
        IPermissionManager permissionManager,
        Func<string, Task<string>> reply,
        bool shouldAllow = false)
    {
        if (shouldAllow is false)
        {
            return CommandExecutionResult.Unauthorized;
        }

        if (message.IslandId != HostEnvs.DodoHostedAdminIsland)
        {
            await reply("该指令只允许在预配置的管理员群组使用");
            return CommandExecutionResult.Failed;
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
            case "islands":
                var openApi = provider.GetRequiredService<OpenApiService>();
                var islands = await openApi.GetIslandListAsync(new GetIslandListInput());

                if (islands is null)
                {
                    await reply.Invoke("获取群组列表失败");
                    return CommandExecutionResult.Failed;
                }

                foreach (var island in islands)
                {
                    messageBuilder.AppendLine($"- {island.IslandName} `{island.IslandId}`");
                }
                
                break;
            default:
                return CommandExecutionResult.Unknown;
        }

        await reply.Invoke(messageBuilder.ToString());

        return CommandExecutionResult.Success;
    }

    public CommandMetadata GetMetadata() => new CommandMetadata(
        CommandName: "system",
        Description: "获取系统信息",
        HelpText: @"""
- {{PREFIX}}system gc    查看系统 GC 信息
- {{PREFIX}}system info    查看系统信息
- {{PREFIX}}system islands    查看 Bot 所在的所有群组
""");

    private static string ToMegabytes(long size)
    {
        return ((double)size / 1024 / 1024).ToString("F");
    }
}
