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
using DodoHosted.Base.Card;
using DodoHosted.Base.Card.Enums;
using DodoHosted.Lib.Plugin.Cards;
using MongoDB.Driver;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

namespace DodoHosted.Lib.Plugin.Builtin;

[AdminIslandOnly]
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
            await context.Reply.Invoke("????????????????????????");
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
                    { "??????", "?????????????????????" },
                    { "Island Name", island.IslandName },
                    { "Island Id", island.IslandId }
                });
                continue;
            }
            
            var allowWebApi = config.AllowUseWebApi ? "???" : "???";
            var enableChannelLogger = config.EnableChannelLogger ? "???" : "???";

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
        [CmdOption("island", "l", "?????? ID")] string islandId)
    {
        return await SetIslandWebApiStatus(collection, context.Reply, islandId, true);
    }
    
    public async Task<bool> DisableIslandWebApi(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection, 
        [CmdOption("island", "l", "?????? ID")] string islandId)
    {
        return await SetIslandWebApiStatus(collection, context.Reply, islandId, false);
    }

    public async Task<bool> SetOrGetIslandWebApiMaxTokenCount(
        CommandContext context,
        [Inject] IMongoCollection<IslandSettings> collection,
        [CmdOption("island", "l", "?????? ID")] string islandId,
        [CmdOption("count", "c", "?????????????????????????????????????????????", false)] int? count)
    {
        var settings = await collection.Find(x => x.IslandId == islandId).FirstOrDefaultAsync();
        if (settings is null)
        {
            await context.Reply.Invoke("???????????????");
            return false;
        }
        
        if (count is null)
        {
            await context.Reply.Invoke($"?????? {islandId} ????????? Token ????????? {settings.MaxWebApiTokenCount}");
            return true;
        }

        var originalValue = settings.MaxWebApiTokenCount;
        settings.MaxWebApiTokenCount = count.Value;
        await collection.ReplaceOneAsync(x => x.IslandId == islandId, settings);
        
        await context.Reply.Invoke($"?????? {islandId} ????????? Token ?????????????????? {settings.MaxWebApiTokenCount} (????????? {originalValue})");

        return true;
    }

    public async Task<bool> GetPluginList(
        CommandContext context,
        [CmdOption("type", "t", "???????????????????????? `enabled` `native` `unloaded`?????????????????????????????????????????????????????? `enabled`", false)] string? type,
        [CmdOption("page", "p", "??????", false)] int? page,
        [Inject] IPluginManager pm,
        [Inject] IPluginLoadingManager plm)
    {
        var displayType = type ?? "enabled";
        var p = Math.Max(page ?? 1, 1);
        const int PageSize = 5;

        CardMessage cardMessage;

        switch (displayType)
        {
            case "enabled" or "e" or "native" or "n":
                var native = displayType is "native" or "n";
                var plugins = pm
                    .GetPlugins(native)
                    .OrderBy(x => x.PluginInfo.Identifier)
                    .Skip(PageSize * (p - 1))
                    .Take(PageSize)
                    .ToArray();
                var maxCount = pm.GetPlugins(native).Count();
                var maxPage = (int)Math.Ceiling(maxCount / (double)PageSize);

                if (plugins.Length == 0)
                {
                    if (p == 1)
                    {
                        await context.Reply.Invoke("????????????????????????");
                    }
                    else
                    {
                        await context.Reply.Invoke("?????????????????????");
                    }

                    return true;
                }
                cardMessage = SystemMessageCard.GetPluginsInfoCard($"Plugins Infos ({p}/{maxPage})", CardTheme.Indigo, plugins);
                break;
            case "unloaded" or "u":
                var (unloadedPluginInfos, failedReadPlugins) = await plm.GetUnloadedPlugins();
                var count = unloadedPluginInfos.Count;
                unloadedPluginInfos = unloadedPluginInfos
                    .OrderBy(x => x.Key)
                    .Skip(PageSize * (p - 1))
                    .Take(PageSize)
                    .ToDictionary(x => x.Key, y => y.Value);
                
                if (unloadedPluginInfos.Count == 0 && failedReadPlugins.Count == 0)
                {
                    if (p == 1)
                    {
                        await context.Reply.Invoke("????????????????????????");
                    }
                    else
                    {
                        await context.Reply.Invoke("?????????????????????????????????");
                    }

                    return true;
                }
                var allPage = (int)Math.Ceiling(count / (double)PageSize);
                var showingPage = Math.Min(allPage, p);
                
                cardMessage = SystemMessageCard.GetUnloadedPluginInfoCard(
                    $"Unloaded Plugins Infos ({showingPage}/{allPage})",
                    CardTheme.Yellow,
                    unloadedPluginInfos,
                    failedReadPlugins);
                break;
            default:
                await context.Reply.Invoke("?????????????????????");
                return false;
        }

        await context.ReplyCard.Invoke(cardMessage);
        return true;
    }

    public async Task<bool> GetPluginInfo(
        CommandContext context,
        [CmdOption("identifier", "i", "???????????????")] string identifier,
        [Inject] IPluginManager pm)
    {
        var manifest = pm.GetPlugin(identifier);
        if (manifest is null)
        {
            await context.Reply.Invoke($"?????? `{identifier}` ?????????");
            return false;
        }

        var card = SystemMessageCard.GetPluginInfoDetailCard("Plugin Detail", CardTheme.Indigo, manifest);
        await context.ReplyCard.Invoke(card);
        
        return true;
    }
    
    public async Task<bool> LoadPlugin(
        CommandContext context,
        [CmdOption("package", "p", "???????????????????????????????????????")] string packageName,
        [Inject] IPluginLoadingManager pluginLoadingManager)
    {
        await pluginLoadingManager.LoadPlugin($"{packageName}.zip");
        await context.Reply.Invoke("???????????????????????????");
        
        return true;
    }
    
    public async Task<bool> UnloadPlugin(
        CommandContext context,
        [CmdOption("identifier", "i", "???????????????")] string identifier,
        [Inject] IPluginLoadingManager pluginLoadingManager)
    {
        var result = pluginLoadingManager.UnloadPlugin(identifier);
        if (result)
        {
            await context.Reply.Invoke("??????????????????");
            return false;
        }

        await context.Reply.Invoke("??????????????????");
        
        return true;
    }
    
    public async Task<bool> ReloadPlugin(CommandContext context,
        [Inject] IPluginLoadingManager pluginLoadingManager)
    {
        pluginLoadingManager.UnloadPlugins();
        await pluginLoadingManager.LoadPlugins();

        await context.Reply.Invoke("?????????????????????");
        
        return true;
    }

    public CommandTreeBuilder GetBuilder()
    {
        return new CommandTreeBuilder("system", "????????????", "system.command.system")
            .Then("gc", "?????? GC ??????", string.Empty, GetGcInfo)
            .Then("info", "??????????????????", string.Empty, GetSystemInfo)
            .Then("islands", "?????? Bot ?????????????????????", string.Empty, GetIslandsInfo)
            .Then("web", "?????? WebAPI ????????????", string.Empty, builder: x => x
                .Then("enable", "?????????????????? WebAPI", string.Empty, EnableIslandWebApi)
                .Then("disable", "?????????????????? WebAPI", string.Empty, DisableIslandWebApi)
                .Then("max", "??????????????????????????? WebAPI Token ??????", string.Empty, SetOrGetIslandWebApiMaxTokenCount))
            .Then("plugin", "??????????????????", string.Empty, builder: x => x
                .Then("list", "??????????????????", string.Empty, GetPluginList)
                .Then("info", "??????????????????", string.Empty, GetPluginInfo)
                .Then("load", "????????????", string.Empty, LoadPlugin)
                .Then("unload", "????????????", string.Empty, UnloadPlugin)
                .Then("reload", "??????????????????", string.Empty, ReloadPlugin));
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
            await reply.Invoke("???????????????");
            return false;
        }

        settings.AllowUseWebApi = type;
        await collection.ReplaceOneAsync(x => x.IslandId == islandId, settings);
        await reply.Invoke("?????????????????????");

        return true;
    }
}
