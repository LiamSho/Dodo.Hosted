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

    public static CardMessage GetPluginsInfoCard(string title, CardTheme theme, params PluginManifest[] pluginManifest)
    {
        var componentGroup = pluginManifest
            .Select(x => x.GetPluginInfoComponents())
            .Aggregate((x, y) => x.Append(new Divider()).Concat(y));
        
        return new CardMessage(new Card { Title = title, Theme = theme, Components = componentGroup.ToList() });
    }

    public static CardMessage GetPluginInfoDetailCard(string title, CardTheme theme, PluginManifest pluginManifest)
    {
        var card = new CardMessage(new Card { Title = title, Theme = theme, Components = new List<ICardComponent>
        {
            new Header("Plugin Info")
        }});
        
        card.AddComponents(pluginManifest.GetPluginInfoComponents());

        if (pluginManifest.Worker.EventHandlers.Length != 0)
        {
            card.AddComponent(new Divider());
            card.AddComponent(new Header("Event Handlers"));
            card.AddComponents(pluginManifest.Worker.EventHandlers
                .Select(x => (ICardComponent)new TextFiled(x.EventHandlerType.FullName!)));
        }
        if (pluginManifest.Worker.CommandExecutors.Length != 0)
        {
            card.AddComponent(new Divider());
            card.AddComponent(new Header("Command Executors"));
            card.AddComponents(pluginManifest.Worker.CommandExecutors
                .Select(x => (ICardComponent)new TextFiled(x.RootNode.Value)));
        }
        if (pluginManifest.Worker.HostedServices.Length != 0)
        {
            card.AddComponent(new Divider());
            card.AddComponent(new Header("Hosted Services"));
            card.AddComponents(pluginManifest.Worker.HostedServices
                .Select(x => (ICardComponent)new TextFiled(x.Name)));
        }

        return card;
    }

    private static IEnumerable<ICardComponent> GetInfoListComponents(this Dictionary<string, string> info)
    {
        return info.Select(x => (ICardComponent)new MultilineText(new Text(x.Key), new Text(x.Value)));
    }

    private static IEnumerable<ICardComponent> GetPluginInfoComponents(this PluginManifest manifest)
    {
        return GetInfoListComponents(new Dictionary<string, string>
        {
            { "Identifier", manifest.PluginInfo.Identifier },
            { "Name", manifest.PluginInfo.Name },
            { "Author", manifest.PluginInfo.Author },
            { "Description", manifest.PluginInfo.Description },
            { "Plugin Version", manifest.PluginInfo.Version },
            { "API Version", manifest.PluginInfo.ApiVersion.ToString() },
            { "Event Handler Count", manifest.Worker.EventHandlers.Length.ToString() },
            { "Command Executor Count", manifest.Worker.CommandExecutors.Length.ToString() },
            { "Hosted Service Count", manifest.Worker.HostedServices.Length.ToString() }
        });
    }
}
