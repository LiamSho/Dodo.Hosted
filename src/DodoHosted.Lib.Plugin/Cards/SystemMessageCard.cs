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

using DodoHosted.Base.Card;
using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.Card.CardComponent;
using DodoHosted.Base.Card.Enums;
using DodoHosted.Lib.Plugin.Models.Module;

// ReSharper disable InvertIf

namespace DodoHosted.Lib.Plugin.Cards;

public static class SystemMessageCard
{
    public static CardMessage GetInfoListCard(string title, CardTheme theme, params Dictionary<string, string>[] infos)
    {
        var componentGroup = infos
            .Select(x => x.GetInfoListComponents())
            .Aggregate((x, y) => x.Append(new Divider()).Concat(y));

        return new CardMessage(new Card { Title = title, Theme = theme, Components = componentGroup.ToList() });
    }

    public static CardMessage GetPluginsInfoCard(string title, CardTheme theme, params PluginModule[] pluginModules)
    {
        var componentGroup = pluginModules
            .Select(x => x.GetPluginInfoComponents())
            .Aggregate((x, y) => x.Append(new Divider()).Concat(y));
        
        return new CardMessage(new Card { Title = title, Theme = theme, Components = componentGroup.ToList() });
    }

    public static CardMessage GetPluginInfoDetailCard(string title, CardTheme theme, PluginModule pluginModule)
    {
        var card = new CardMessage(new Card { Title = title, Theme = theme, Components = new List<ICardComponent>
        {
            new Header("Plugin Info")
        }});
        
        card.AddComponents(pluginModule.GetPluginInfoComponents());

        return card;
    }

    private static IEnumerable<ICardComponent> GetInfoListComponents(this Dictionary<string, string> info)
    {
        return info.Select(x => (ICardComponent)new MultilineText(new Text(x.Key), new Text(x.Value)));
    }

    private static IEnumerable<ICardComponent> GetPluginInfoComponents(this PluginModule module)
    {
        return GetInfoListComponents(new Dictionary<string, string>
        {
            { "Identifier", module.PluginInfo.Identifier },
            { "Name", module.PluginInfo.Name },
            { "Author", module.PluginInfo.Author },
            { "Description", module.PluginInfo.Description },
            { "Plugin Version", module.PluginInfo.Version },
            { "API Version", module.PluginInfo.ApiVersion.ToString() },
            { "Event Handler Count", module.EventHandlerModule.Count().ToString() },
            { "Command Executor Count", module.CommandExecutorModule.Count().ToString() },
            { "Hosted Service Count", module.HostedServiceModule.Count().ToString() },
            { "Web Handler Count", module.WebHandlerModule.Count().ToString() }
        });
    }
}
