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
using DoDo.Open.Sdk.Models.Islands;
using DodoHosted.Base.App.Attributes;
using DodoHosted.Base.App.Context;
using DodoHosted.Base.Card.Enums;
using DodoHosted.Base.Context;
using DodoHosted.Lib.Plugin.Cards;
using MongoDB.Driver;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

namespace DodoHosted.Lib.Plugin.Builtin;

public sealed class SystemCommand : ICommandExecutor
{
    public async Task<bool> GetGcInfo(CommandContext context)
    {
        var gcInfo = GC.GetGCMemoryInfo();

        var infos = new Dictionary<string, string>
        {
            { "Max Generation", GC.MaxGeneration.ToString() },
            { "Finalization Pending Count", gcInfo.FinalizationPendingCount.ToString() },
            { "Pinned Objects Count", gcInfo.PinnedObjectsCount.ToString() },
            { "Total Heap Size", $"{ToMegabytes(gcInfo.HeapSizeBytes)} MB" },
            { "Committed Heap Size", $"{ToMegabytes(gcInfo.TotalCommittedBytes)} MB" },
            { "Memory Load", $"{ToMegabytes(gcInfo.MemoryLoadBytes)} MB" },
            { "Total Allocated Memory", $"{ToMegabytes(GC.GetTotalMemory(false))} MB" }
        };

        var card = SystemMessageCard.GetInfoListCard("Garbage Collector Info", CardTheme.Indigo, infos);
        await context.ReplyCard.Invoke(card);
        
        return true;
    }

    public async Task<bool> GetSystemInfo(CommandContext context)
    {
        var infos = new Dictionary<string, string>
        {
            { "System Time", $"{DateTimeOffset.UtcNow.AddHours(8):u}" },
            { "DodoHosted Version", HostEnvs.DodoHostedVersion },
            { "Containerized", HostEnvs.DodoHostedInContainer.ToString() },
            { ".NET Runtime Framework", RuntimeInformation.FrameworkDescription },
            { ".NET Runtime Identifier", RuntimeInformation.RuntimeIdentifier }
        };
        
        var card = SystemMessageCard.GetInfoListCard("System Info", CardTheme.Indigo, infos);
        
        await context.ReplyCard.Invoke(card);
        
        return true;
    }

    public async Task<bool> GetIslandsInfo(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection,
        [Inject] OpenApiService openApiService)
    {
        var islands = await openApiService.GetIslandListAsync(new GetIslandListInput());
        
        if (islands is null)
        {
            await context.Reply.Invoke("获取群组列表失败");
            return false;
        }

        var infos = new List<Dictionary<string, string>>();
        
        foreach (var island in islands)
        {
            var config = await collection.Find(x => x.IslandId == island.IslandId).FirstOrDefaultAsync();

            if (config is null)
            {
                infos.Add(new Dictionary<string, string>
                {
                    { "错误", "找不到群组配置" },
                    { "Island Name", island.IslandName },
                    { "Island Id", island.IslandId }
                });
                continue;
            }
            
            var allowWebApi = config.AllowUseWebApi ? "✅" : "❌";
            var enableChannelLogger = config.EnableChannelLogger ? "✅" : "❌";

            infos.Add(new Dictionary<string, string>
            {
                { "Island Name", island.IslandName },
                { "Island Id", island.IslandId },
                { "Web API Status", allowWebApi },
                { "Web API Token Count", config.WebApiToken.Count.ToString() },
                { "Channel Logger", enableChannelLogger },
            });
        }
        
        var card = SystemMessageCard.GetInfoListCard("Islands Infos", CardTheme.Indigo, infos.ToArray());
        
        await context.ReplyCard.Invoke(card);
        
        return true;
    }

    public async Task<bool> EnableIslandWebApi(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection, 
        [CmdOption("island", "l", "群组 ID")] string islandId)
    {
        return await SetIslandWebApiStatus(collection, context.Reply, islandId, true);
    }
    
    public async Task<bool> DisableIslandWebApi(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection, 
        [CmdOption("island", "l", "群组 ID")] string islandId)
    {
        return await SetIslandWebApiStatus(collection, context.Reply, islandId, false);
    }

    public async Task<bool> SetOrGetIslandWebApiMaxTokenCount(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection,
        [CmdOption("island", "l", "群组 ID")] string islandId,
        [CmdOption("count", "c", "最大数量，不传为获取最大数量值", false)] int? count)
    {
        var settings = await collection.Find(x => x.IslandId == islandId).FirstOrDefaultAsync();
        if (settings is null)
        {
            await context.Reply.Invoke("群组不存在");
            return false;
        }
        
        if (count is null)
        {
            await context.Reply.Invoke($"群组 {islandId} 的最大 Token 数量为 {settings.MaxWebApiTokenCount}");
            return true;
        }

        var originalValue = settings.MaxWebApiTokenCount;
        settings.MaxWebApiTokenCount = count.Value;
        await collection.ReplaceOneAsync(x => x.IslandId == islandId, settings);
        
        await context.Reply.Invoke($"群组 {islandId} 的最大 Token 数量已设置为 {settings.MaxWebApiTokenCount} (原先为 {originalValue})");

        return true;
    }

    public async Task<bool> GetPluginList(
        CommandContext context,
        [CmdOption("native", "n", "显示 Native 类型", false)] bool? native,
        [CmdOption("page", "p", "页码", false)] int? page,
        [Inject] IPluginManager pm)
    {
        var p = Math.Max(page ?? 1, 1);

        const int PageSize = 5;
        
        var plugins = pm.GetPlugins(native ?? false)
            .OrderBy(x => x.PluginInfo.Identifier)
            .Skip(PageSize * (p - 1))
            .Take(PageSize)
            .ToArray();
        var maxCount = pm.GetPlugins(native ?? false).Count();
        var maxPage = (int)Math.Ceiling(maxCount / (double)PageSize);

        if (plugins.Length == 0)
        {
            if (p == 1)
            {
                await context.Reply.Invoke("没有已载入的插件");
            }
            else
            {
                await context.Reply.Invoke("没有更多插件了");
            }

            return true;
        }

        var card = SystemMessageCard.GetPluginsInfoCard($"Plugins Infos ({p}/{maxPage})", CardTheme.Indigo, plugins);
        await context.ReplyCard.Invoke(card);

        return true;
    }

    public async Task<bool> GetPluginInfo(
        CommandContext context,
        [CmdOption("identifier", "i", "插件标识符")] string identifier,
        [Inject] IPluginManager pm)
    {
        var manifest = pm.GetPlugin(identifier);
        if (manifest is null)
        {
            await context.Reply.Invoke($"插件 `{identifier}` 不存在");
            return false;
        }

        var card = SystemMessageCard.GetPluginInfoDetailCard("Plugin Detail", CardTheme.Indigo, manifest);
        await context.ReplyCard.Invoke(card);
        
        return true;
    }
    
    public async Task<bool> LoadPlugin(
        CommandContext context,
        [CmdOption("package", "p", "插件包文件名（不含扩展名）")] string packageName,
        [Inject] IPluginLoadingManager pluginLoadingManager)
    {
        await pluginLoadingManager.LoadPlugin($"{packageName}.zip");
        await context.Reply.Invoke("已执行插件加载任务");
        
        return true;
    }
    
    public async Task<bool> UnloadPlugin(
        CommandContext context,
        [CmdOption("identifier", "i", "插件标识符")] string identifier,
        [Inject] IPluginLoadingManager pluginLoadingManager)
    {
        var result = pluginLoadingManager.UnloadPlugin(identifier);
        if (result)
        {
            await context.Reply.Invoke("插件卸载成功");
            return false;
        }

        await context.Reply.Invoke("插件卸载失败");
        
        return true;
    }
    
    public async Task<bool> ReloadPlugin(CommandContext context,
        [Inject] IPluginLoadingManager pluginLoadingManager)
    {
        pluginLoadingManager.UnloadPlugins();
        await pluginLoadingManager.LoadPlugins();

        await context.Reply.Invoke("已执行重载任务");
        
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
                .Then("disable", "禁止群组使用 WebAPI", string.Empty, DisableIslandWebApi)
                .Then("max", "设置或查看群组最大 WebAPI Token 数量", string.Empty, SetOrGetIslandWebApiMaxTokenCount))
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
    private static async Task<bool> SetIslandWebApiStatus(IMongoCollection<IslandSettings> collection, ContextBase.Reply reply, string islandId, bool type)
    {
        var settings = await collection.Find(x => x.IslandId == islandId).FirstOrDefaultAsync();
        if (settings is null)
        {
            await reply.Invoke("群组不存在");
            return false;
        }

        settings.AllowUseWebApi = type;
        await collection.ReplaceOneAsync(x => x.IslandId == islandId, settings);
        await reply.Invoke("已更新群组设置");

        return true;
    }
}
