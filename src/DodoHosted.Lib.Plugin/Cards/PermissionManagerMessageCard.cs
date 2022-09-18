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
// ReSharper disable InvertIf

namespace DodoHosted.Lib.Plugin.Cards;

public static class PermissionManagerMessageCard
{
    public static CardMessage GetPermissionCheckResultCard(this PermissionSchema? schema,
        IEnumerable<GetRoleListOutput> roleList, IEnumerable<GetChannelListOutput> channelList)
    {
        var result = schema?.Value == "allow" ? "Allow" : "Deny";

        var card = new CardMessage(new Card
        {
            Title = "权限检查结果",
            Theme = result == "Allow" ? CardTheme.Green : CardTheme.Red,
            Components = new List<ICardComponent>
            {
                new TextFiled($"检查结果：`{result}`"),
                new Divider()
            }
        });

        if (schema is null)
        {
            card.AddComponent(new TextFiled("无匹配规则"));
        }
        else
        {
            var components = schema.GetPermissionSchemaDescriptionCardComponents(roleList, channelList);
            card.AddComponents(components);
        }
        
        return card;
    }

    public static CardMessage GetPermissionSingleCard(this PermissionSchema? schema, string title,
        IEnumerable<GetRoleListOutput> roleList, IEnumerable<GetChannelListOutput> channelList)
    {
        var card = new CardMessage(new Card
        {
            Title = title,
            Theme = CardTheme.Default,
            Components = new List<ICardComponent>()
        });

        if (schema is null)
        {
            card.AddComponent(new TextFiled("找不到该权限"));
            return card;
        }

        var components = schema.GetPermissionSchemaDescriptionCardComponents(roleList, channelList);
        card.AddComponents(components);
        
        return card;
    }
    
    public static CardMessage GetPermissionListCard(this IEnumerable<PermissionSchema> schemas, string title,
        IEnumerable<GetRoleListOutput> roleList, IEnumerable<GetChannelListOutput> channelList,
        int? totalPages = null, int? currentPage = null)
    {
        var t = title;
        if (totalPages is not null && currentPage is not null)
        {
            t += $" - {currentPage}/{totalPages}";
        }
        
        var card = new CardMessage(new Card
        {
            Title = t,
            Theme = CardTheme.Default, Components = new List<ICardComponent>
            {
                new TextFiled("***<GUID>***"),
                new TextFiled("<节点名> `<值>` `<频道>` `<身份组>`"),
                new Divider()
            }
        });

        var components = schemas
            .SelectMany(x => x.GetPermissionSchemaCompactDescriptionCardComponents(roleList, channelList));
        card.AddComponents(components);
        
        return card;
    }

    public static CardMessage GetPermissionChangeResultCard(PermissionSchema before, PermissionSchema after,
        List<GetRoleListOutput> roleList, List<GetChannelListOutput> channelList)
    {
        var card = new CardMessage(new Card
        {
            Title = "权限更新", Theme = CardTheme.Default, Components = new List<ICardComponent>()
        });

        var newPermissionCardComponents = after.GetPermissionSchemaDescriptionCardComponents(roleList, channelList);

        card.AddComponent(new Header("更新项目"));
        var updated = false;
        if (before.Channel != after.Channel)
        {
            card.AddComponent(new TextFiled($"- 频道：{before.GetChannelString(channelList)}"));
            card.AddComponent(new TextFiled($"+ 频道：{after.GetChannelString(channelList)}"));
            card.AddComponent(new Divider());
            updated = true;
        }
        if (before.Role != after.Role)
        {
            card.AddComponent(new TextFiled($"- 身份组：{before.GetRoleString(roleList)}"));
            card.AddComponent(new TextFiled($"+ 身份组：{after.GetRoleString(roleList)}"));
            card.AddComponent(new Divider());
            updated = true;
        }
        if (before.Value != after.Value)
        {
            card.AddComponent(new TextFiled($"- 值：{before.Value}"));
            card.AddComponent(new TextFiled($"+ 值：{after.Value}"));
            card.AddComponent(new Divider());
            updated = true;
        }

        if (updated is false)
        {
            card.AddComponent(new TextFiled("未更新任何项目"));
            card.AddComponent(new Divider());
        }
        
        card.AddComponents(newPermissionCardComponents);

        return card;
    }
    
    private static IEnumerable<ICardComponent> GetPermissionSchemaDescriptionCardComponents(this PermissionSchema schema,
        IEnumerable<GetRoleListOutput> roleList, IEnumerable<GetChannelListOutput> channelList)
    {
        var components = new List<ICardComponent>
        {
            new MultilineText(new Text("GUID"), new Text(schema.Id.ToString())),
            new MultilineText(new Text("节点"), new Text(schema.Node)),
            new MultilineText(new Text("频道"), new Text(schema.GetChannelString(channelList))),
            new MultilineText(new Text("身份组"), new Text(schema.GetRoleString(roleList))),
            new MultilineText(new Text("配置值"), new Text(schema.Value))
        };

        return components;
    }

    private static IEnumerable<ICardComponent> GetPermissionSchemaCompactDescriptionCardComponents(this PermissionSchema schema,
        IEnumerable<GetRoleListOutput> roleList, IEnumerable<GetChannelListOutput> channelList)
    {
        return new ICardComponent[]
        {
            new TextFiled($"***{schema.Id.ToString()}***"),
            new TextFiled($"{schema.Node} " +
                          $"`{schema.Value}` " +
                          $"`{schema.GetChannelString(channelList)}` " +
                          $"`{schema.GetRoleString(roleList)}`")
        };
    }

    private static string GetChannelString(this PermissionSchema schema, IEnumerable<GetChannelListOutput> channelList)
    {
        var channel = "*";

        if (schema.Channel != "*")
        {
            var c = channelList.FirstOrDefault(x => x.ChannelId == schema.Channel);
            channel = c is null ? $"<未知的频道> ({schema.Channel})" : $"{c.ChannelName} ({schema.Channel})";
        }

        return channel;
    }

    private static string GetRoleString(this PermissionSchema schema, IEnumerable<GetRoleListOutput> roleList)
    {
        var role = "*";
        
        if (schema.Role != "*")
        {
            var r = roleList.FirstOrDefault(x => x.RoleId == schema.Role);
            role = r is null ? $"<未知的身份组> ({schema.Role})" : $"{r.RoleName} ({schema.Role})";
        }

        return role;
    }
}
