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

using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Roles;
using DodoHosted.Base.Card;
using DodoHosted.Base.Card.BaseComponent;
using DodoHosted.Base.Card.CardComponent;
using DodoHosted.Base.Card.Enums;

namespace DodoHosted.Lib.Plugin.Cards;

public static class IslandMessageCard
{
    public static CardMessage GetRolesCard(this List<GetRoleListOutput> roles)
    {
        var card = new CardMessage(new Card
        {
            Title = "群组身份组",
            Theme = CardTheme.Default,
            Components = new List<ICardComponent>
            {
                new MultilineText(
                    new Paragraph(4, new List<Text>
                    {
                        new("序号"),
                        new("ID"),
                        new("色彩"),
                        new("名称")
                    })),
                new Divider()
            }
        });
        
        var positionMaxLength = roles.Select(x => x.Position.ToString().Length).Max();
        var maxPosition = roles.Select(x => x.Position).Max();
                 
        foreach (var role in roles.OrderByDescending(x => x.Position))
        {
            var descPosition = maxPosition - role.Position + 1;
            card.AddComponent(new MultilineText(
                new Paragraph(4, new List<Text>
                {
                    new(FormatLength(descPosition.ToString(), positionMaxLength, '0')),
                    new(role.RoleId),
                    new(role.RoleColor),
                    new(role.RoleName)
                })));
        }

        return card;
    }

    public static async Task<CardMessage> GetIslandSettingsCard(this IslandSettings settings, OpenApiService openApiService)
    {
        var loggerChannel = new DodoChannelId(settings.LoggerChannelId);
        var loggerChannelName = "<未设置>";
        var loggerChannelId = loggerChannel.Valid ? loggerChannel.Value : "<未设置>";
        // ReSharper disable once InvertIf
        if (loggerChannel.Valid)
        {
            var response = await openApiService.GetChannelInfoAsync(new GetChannelInfoInput
            {
                ChannelId = settings.LoggerChannelId
            });
            loggerChannelName = response is null ? "<获取频道名失败>" : response.ChannelName;
        }

        return new CardMessage(new Card
        {
            Title = "群组设置",
            Theme = CardTheme.Default,
            Components = new List<ICardComponent>
            {
                new MultilineText(new Text("项"), new Text("值")),
                new Divider(),
                new MultilineText(new Text("群组 ID"), new Text(settings.IslandId)),
                new MultilineText(new Text("日志频道 ID"), new Text(loggerChannelId)),
                new MultilineText(new Text("日志频道名"), new Text(loggerChannelName)),
                new MultilineText(new Text("日志频道启用状态"), new Text(settings.EnableChannelLogger ? "Enabled" : "Disabled"))
            }
        });
    }
    
    private static string FormatLength(string str, int maxLength, char placeHolder = ' ')
    {
        var s = str;
        while (s.Length < maxLength)
        {
            s = placeHolder + s;
        }

        return s;
    }
}
