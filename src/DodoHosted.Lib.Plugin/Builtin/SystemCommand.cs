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
            case "plugin":
                var pm = provider.GetRequiredService<IPluginManager>();
                return await RunPluginCommand(args, pm, reply);
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
- `{{PREFIX}}system gc`    查看系统 GC 信息
- `{{PREFIX}}system info`    查看系统信息
- `{{PREFIX}}system islands`    查看 Bot 所在的所有群组
- `{{PREFIX}}system plugin list`    查看插件列表
- `{{PREFIX}}system plugin info <插件标识符>`    查看插件信息
- `{{PREFIX}}system plugin load <插件包文件名>`    启用插件
- `{{PREFIX}}system plugin unload <插件标识符>`    禁用插件
""");

    private static string ToMegabytes(long size)
    {
        return ((double)size / 1024 / 1024).ToString("F");
    }

    private static async Task<CommandExecutionResult> RunPluginCommand(
        string[] args,
        IPluginManager pluginManager,
        Func<string, Task<string>> reply)
    {
        var operation = args.Skip(2).FirstOrDefault();
        var param = args.Skip(3).FirstOrDefault();
        
        if (operation is null)
        {
            return CommandExecutionResult.Unknown;
        }
        if (operation is not "list" && param is null)
        {
            return CommandExecutionResult.Unknown;
        }
        var messageBuilder = new StringBuilder();        
        switch (operation)
        {
            case "list":
                var (allPluginInfos, failLoadedPlugins) = await pluginManager.GetAllPluginInfos();
                foreach (var info in allPluginInfos)
                {
                    var enabled = info.Value == string.Empty ? "True" : $"False ({info.Value})";
                    messageBuilder.AppendLine($"- `{info.Key.Identifier}`");
                    messageBuilder.AppendLine($"    $ 名称: {info.Key.Name}");
                    messageBuilder.AppendLine($"    $ 作者: {info.Key.Author}");
                    messageBuilder.AppendLine($"    $ 描述: {info.Key.Description}");
                    messageBuilder.AppendLine($"    $ 版本: {info.Key.Version}");
                    messageBuilder.AppendLine($"    $ 启用: {enabled}");
                }
                foreach (var failLoadedPlugin in failLoadedPlugins)
                {
                    messageBuilder.AppendLine($"- 获取信息失败：`{failLoadedPlugin}`");
                }
                break;
            case "info":
                var manifest = pluginManager.GetPluginManifest(param!);
                if (manifest is null)
                {
                    await reply.Invoke($"插件 `{param}` 不存在");
                    return CommandExecutionResult.Failed;
                }
                messageBuilder.AppendLine($"- 标识符：{manifest.PluginInfo.Identifier}");
                messageBuilder.AppendLine($"- 名称：{manifest.PluginInfo.Name}");
                messageBuilder.AppendLine($"- 作者：{manifest.PluginInfo.Author}");
                messageBuilder.AppendLine($"- 描述：{manifest.PluginInfo.Description}");
                messageBuilder.AppendLine($"- 版本：{manifest.PluginInfo.Version}");
                messageBuilder.AppendLine($"- 事件处理器 ({manifest.EventHandlers.Length} 个):");
                foreach (var handler in manifest.EventHandlers)
                {
                    messageBuilder.AppendLine($"    $ `{handler.EventHandlerType.Name}`");
                }
                
                messageBuilder.AppendLine($"- 指令处理器 ({manifest.CommandManifests.Length} 个):");
                foreach (var command in manifest.CommandManifests)
                {
                    messageBuilder.AppendLine($"    $ `{command.Name}`");
                }
                
                break;
            case "load":
                await pluginManager.LoadPlugin(param!);
                await reply.Invoke("已执行插件加载任务");
                return CommandExecutionResult.Success;
            case "unload":
                pluginManager.UnloadPlugin(param!);
                var unloadSuccess = pluginManager.GetPluginManifest(param!) is null;
                if (unloadSuccess)
                {
                    await reply.Invoke("插件卸载成功");
                    return CommandExecutionResult.Success;
                }

                await reply.Invoke("插件卸载失败");
                return CommandExecutionResult.Failed;
            default:
                return CommandExecutionResult.Unknown;
        }

        await reply.Invoke(messageBuilder.ToString());
        return CommandExecutionResult.Success;
    }
}
