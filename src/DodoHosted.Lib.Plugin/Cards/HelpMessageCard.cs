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
using DodoHosted.Base.Card.CardComponent;
using DodoHosted.Base.Card.Enums;

namespace DodoHosted.Lib.Plugin.Cards;

public static class HelpMessageCard
{
    public static async Task<CardMessage> GetCommandHelpMessage(
        this CommandNode node,
        IParameterResolver parameterResolver,
        PluginBase.PermissionCheck permissionChecker)
    {
        var card = new CardMessage(new Card
        {
            Title = "指令帮助",
            Theme = CardTheme.Default,
            Components = new List<ICardComponent>()
        });
        
        if (await permissionChecker.Invoke(node.PermissionNode) is false)
        {
            card.AddComponent(new TextFiled("你没有权限使用此指令"));
            return card;
        }
        
        card.AddComponent(new Header($"{node.Value} - {node.Description}"));
        card.AddComponent(new TextFiled($"权限：`{node.PermissionNode}`"));

        if (node.Options.Count != 0)
        {
            card.AddComponent(new Divider());
            
            foreach (var (_, (type, cmdOption)) in node.Options)
            {
                var title = $"--{cmdOption.Name}";
                if (string.IsNullOrEmpty(cmdOption.Abbr) is false)
                {
                    title += $" | -{cmdOption.Abbr}";
                }

                var attrs = new List<string>
                {
                    cmdOption.Required ? "`必填`" : "`可选`",
                    $"`{parameterResolver.GetDisplayParameterTypeName(type)}`"
                };

                card.AddComponent(new Header(title));
                card.AddComponent(new TextFiled($"{string.Join(" ", attrs)} {cmdOption.Description}"));
            }
        }

        var children = node.GetChildren().ToArray();
        
        if (children.Length == 0)
        {
            return card;
        }

        card.AddComponent(new Divider());
        foreach (var child in children)
        {
            card.AddComponent(new Header(string.Join(" ", child.Value)));
            card.AddComponent(new TextFiled($"`{child.PermissionNode}` {child.Description}"));
        }
        
        return card;
    }

    public static async Task<CardMessage> GetCommandListMessage(this IEnumerable<CommandManifest> manifests, PluginBase.PermissionCheck permissionChecker)
    {
        var card = new CardMessage
        {
            Content = string.Empty,
            Card = new Card
            {
                Title = "指令列表",
                Theme = CardTheme.Default,
                Components = new List<ICardComponent>()
            }
        };

        foreach (var manifest in manifests)
        {
            if (await permissionChecker.Invoke(manifest.RootNode.PermissionNode) is false)
            {
                continue;
            }
            
            card.AddComponent(new Header($"{HostEnvs.CommandPrefix}{manifest.RootNode.Value}"));
            card.AddComponent(new TextFiled(manifest.RootNode.Description));
        }

        if (card.Card.Components.Count == 0)
        {
            card.AddComponent(new TextFiled("没有可用的指令"));
        }
        
        return card;
    }
}
