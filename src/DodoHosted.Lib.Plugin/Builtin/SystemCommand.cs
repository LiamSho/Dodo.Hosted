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
using DodoHosted.Base;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Entities;
using DodoHosted.Base.Command.Attributes;
using DodoHosted.Base.Command.Builder;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

namespace DodoHosted.Lib.Plugin.Builtin;

public class SystemCommand : ICommandExecutor
{
    public async Task<bool> GetGcInfo(PluginBase.Context context)
    {
        var messageBuilder = new StringBuilder();

        var gcInfo = GC.GetGCMemoryInfo();
        
        messageBuilder.AppendLine($"Max Generation: `{GC.MaxGeneration}`");
        messageBuilder.AppendLine($"Finalization Pending Count: `{gcInfo.FinalizationPendingCount}`");
        messageBuilder.AppendLine($"Pinned Objects Count: `{gcInfo.PinnedObjectsCount}`");
        messageBuilder.AppendLine($"Total Heap Size: `{ToMegabytes(gcInfo.HeapSizeBytes)} MB`");
        messageBuilder.AppendLine($"Committed Heap Size: `{ToMegabytes(gcInfo.TotalCommittedBytes)} MB`");
        messageBuilder.AppendLine($"Memory Load: `{ToMegabytes(gcInfo.MemoryLoadBytes)}` MB");
        messageBuilder.AppendLine($"Total Allocated Memory: `{ToMegabytes(GC.GetTotalMemory(false))} MB`");
        
        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        
        return true;
    }

    public async Task<bool> GetSystemInfo(PluginBase.Context context)
    {
        var messageBuilder = new StringBuilder();
        
        messageBuilder.AppendLine($"System Time: `{DateTimeOffset.UtcNow.AddHours(8).ToString("u")}`");
        messageBuilder.AppendLine($"DodoHosted Version: `{HostEnvs.DodoHostedVersion}`");
        messageBuilder.AppendLine($"Containerized: `{HostEnvs.DodoHostedInContainer.ToString()}`");
        messageBuilder.AppendLine($".NET Runtime Framework: `{RuntimeInformation.FrameworkDescription}`");
        messageBuilder.AppendLine($".NET Runtime Identifier: `{RuntimeInformation.RuntimeIdentifier}`");
        
        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        
        return true;
    }

    public async Task<bool> GetIslandsInfo(PluginBase.Context context)
    {
        var col = context.Provider.GetRequiredService<IMongoDatabase>()
            .GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);
                
        var islands = await context.OpenApiService.GetIslandListAsync(new GetIslandListInput());
                
        if (islands is null)
        {
            await context.Functions.Reply.Invoke("获取群组列表失败");
            return false;
        }

        var messageBuilder = new StringBuilder();
        
        foreach (var island in islands)
        {
            var config = await col.Find(x => x.IslandId == island.IslandId).FirstOrDefaultAsync();
            var allowWebApi = config is null ? "未配置" : config.AllowUseWebApi ? "✅" : "❌";
            var enableChannelLogger = config is null ? "未配置" : config.EnableChannelLogger ? "✅" : "❌";
            messageBuilder.AppendLine($"- {island.IslandName} `{island.IslandId}`");
            messageBuilder.AppendLine($"  $ Web API: {allowWebApi}");
            messageBuilder.AppendLine($"  $ Channel Logger: {enableChannelLogger}");
        }
        
        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        
        return true;
    }

    public async Task<bool> EnableIslandWebApi(
        PluginBase.Context context,
        [CmdOption("island", "l", "群组 ID")] string islandId)
    {
        return await SetIslandWebApiStatus(context, islandId, true);
    }
    
    public async Task<bool> DisableIslandWebApi(
        PluginBase.Context context,
        [CmdOption("island", "l", "群组 ID")] string islandId)
    {
        return await SetIslandWebApiStatus(context, islandId, false);
    }

    public async Task<bool> GetPluginList(PluginBase.Context context)
    {
        var pm = GetPluginManager(context);
        
        var (allPluginInfos, failLoadedPlugins) = await pm.GetAllPluginInfos();

        if (allPluginInfos.Count == 0 && failLoadedPlugins.Count == 0)
        {
            await context.Functions.Reply.Invoke("没有已载入的插件");
            return true;
        }
        
        var messageBuilder = new StringBuilder();
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

        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        
        return true;
    }

    public async Task<bool> GetPluginInfo(
        PluginBase.Context context,
        [CmdOption("identifier", "i", "插件标识符")] string identifier)
    {
        var pm = GetPluginManager(context);
        
        var manifest = pm.GetPluginManifest(identifier);
        if (manifest is null)
        {
            await context.Functions.Reply.Invoke($"插件 `{identifier}` 不存在");
            return false;
        }
        
        var messageBuilder = new StringBuilder();
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
            messageBuilder.AppendLine($"    $ `{command.RootNode.Value}`");
        }

        await context.Functions.Reply.Invoke(messageBuilder.ToString());
        
        return true;
    }
    
    public async Task<bool> LoadPlugin(
        PluginBase.Context context,
        [CmdOption("package", "p", "插件包文件名（不含扩展名）")] string packageName)
    {
        var pm = GetPluginManager(context);
        
        await pm.LoadPlugin($"{packageName}.zip");
        await context.Functions.Reply.Invoke("已执行插件加载任务");
        
        return true;
    }
    
    public async Task<bool> UnloadPlugin(
        PluginBase.Context context,
        [CmdOption("identifier", "i", "插件标识符")] string identifier)
    {
        var pm = GetPluginManager(context);
        
        pm.UnloadPlugin(identifier);
        var unloadSuccess = pm.GetPluginManifest(identifier) is null;
        if (unloadSuccess)
        {
            await context.Functions.Reply.Invoke("插件卸载成功");
            return false;
        }

        await context.Functions.Reply.Invoke("插件卸载失败");
        
        return true;
    }
    
    public async Task<bool> ReloadPlugin(PluginBase.Context context)
    {
        var pm = GetPluginManager(context);
        
        pm.UnloadPlugins();
        await pm.LoadPlugins();

        await context.Functions.Reply.Invoke("已执行重载任务");
        
        return true;
    }

    public CommandTreeBuilder GetBuilder()
    {
        return new CommandTreeBuilder("system", "系统指令", "system.command.system")
            .Then("gc", "获取 GC 信息", string.Empty, GetGcInfo)
            .Then("info", "获取系统信息", string.Empty, GetSystemInfo)
            .Then("islands", "获取 Bot 加入的群组信息", string.Empty, GetIslandsInfo)
            .Then("web", "群组 WebAPI 使用控制", string.Empty, builder: x => x
                .Then("enable", "允许群组使用 WebAPI", string.Empty, EnableIslandWebApi)
                .Then("disable", "禁止群组使用 WebAPI", string.Empty, DisableIslandWebApi))
            .Then("plugin", "插件管理指令", string.Empty, builder: x => x
                .Then("list", "获取插件列表", string.Empty, GetPluginList)
                .Then("info", "获取插件信息", string.Empty, GetPluginInfo)
                .Then("load", "加载插件", string.Empty, LoadPlugin)
                .Then("unload", "卸载插件", string.Empty, UnloadPlugin)
                .Then("reload", "重载所有插件", string.Empty, ReloadPlugin));
    }
    
    private static string ToMegabytes(long size)
    {
        return ((double)size / 1024 / 1024).ToString("F");
    }
    private static IPluginManager GetPluginManager(PluginBase.Context context)
    {
        return context.Provider.GetRequiredService<IPluginManager>();
    }
    private static async Task<bool> SetIslandWebApiStatus(PluginBase.Context context, string islandId, bool type)
    {
        var mongoCollection = context.Provider.GetRequiredService<IMongoDatabase>()
            .GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);

        var settings = await mongoCollection.Find(x => x.IslandId == islandId).FirstOrDefaultAsync();
        if (settings is null)
        {
            await context.Functions.Reply.Invoke("群组不存在");
            return false;
        }

        settings.AllowUseWebApi = type;
        await mongoCollection.ReplaceOneAsync(x => x.IslandId == islandId, settings);
        await context.Functions.Reply.Invoke("已更新群组设置");

        return true;
    }
}
