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

using DoDo.Open.Sdk.Models.Islands;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base.App.Entities;
using DodoHosted.Base.App.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;

namespace DodoHosted.Base.App.Web;

[ApiController]
[Route("plugin")]
public class PluginSystemController : ControllerBase
{
    [HttpPost("{pluginIdentifier}/{name}")]
    public async Task<IActionResult> PluginOperation(
        [FromRoute] string pluginIdentifier,
        [FromRoute] string name,
        [FromHeader(Name = "dodo-hosted-api-token")] string? token,
        [FromHeader(Name = "dodo-hosted-island")] string? islandId,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] IWebRequestManager webRequestManager,
        [FromServices] IMongoDatabase mongoDatabase,
        [FromServices] OpenApiService openApiService,
        [FromServices] IChannelLogger channelLogger)
    {
        if (httpContextAccessor.HttpContext is null)
        {
            await channelLogger.LogCritical(HostEnvs.DodoHostedAdminIsland,
                $"`HttpContextAccessor.HttpContext` 为空，插件 Identifier: `{pluginIdentifier}`");
            return Problem(statusCode: StatusCodes.Status500InternalServerError);
        }

        var collection = mongoDatabase.GetCollection<IslandSettings>(HostConstants.MONGO_COLLECTION_ISLAND_SETTINGS);
        var island = await collection.Find(x => x.IslandId == islandId).FirstOrDefaultAsync();
        string realIslandId;

        if (island?.AllowUseWebApi is not true)
        {
            return Forbid();
        }
        
        if (token != island.WebApiToken)
        {
            if (token != HostEnvs.DodoHostedWebMasterToken)
            {
                return Unauthorized(null);
            }

            var islandInfo = await openApiService.GetIslandInfoAsync(new GetIslandInfoInput { IslandId = island.IslandId });
            
            realIslandId = "*";
            await channelLogger.LogWarning(HostEnvs.DodoHostedAdminIsland,
                $"源自 `{httpContextAccessor.HttpContext.Connection.RemoteIpAddress}` 的请求使用了 Master Token，" +
                $"群组: `{islandInfo.IslandName}({islandId})`， 插件 Identifier: `{pluginIdentifier}`");
        }
        else
        {
            realIslandId = island.IslandId;
        }

        var response = await webRequestManager.HandleRequestAsync
            (pluginIdentifier, realIslandId, name, httpContextAccessor.HttpContext.Request);

        return response;
    }
}
