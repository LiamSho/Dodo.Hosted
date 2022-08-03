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

using System.Text.Json;
using DoDo.Open.Sdk.Models.Members;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base;
using DodoHosted.Base.Core.Notifications;
using DodoHosted.Base.Interfaces;
using DodoHosted.Base.Models;
using DodoHosted.Open.Plugin;
using MediatR;
using StackExchange.Redis;

namespace DodoHosted.Lib.Plugin;

public class CommandNotificationListener : INotificationHandler<DodoChannelMessageNotification<MessageBodyText>>
{
    private readonly OpenApiService _apiService;
    private readonly IChannelLogger _channelLogger;
    private readonly IPluginManager _pluginManager;
    private readonly IServiceProvider _provider;
    private readonly IDatabase _redis;

    public CommandNotificationListener(
        OpenApiService apiService,
        IChannelLogger channelLogger,
        IPluginManager pluginManager,
        IServiceProvider provider,
        IDatabase redis)
    {
        _apiService = apiService;
        _channelLogger = channelLogger;
        _pluginManager = pluginManager;
        _provider = provider;
        _redis = redis;
    }
    
    public async Task Handle(DodoChannelMessageNotification<MessageBodyText> notification, CancellationToken cancellationToken)
    {
        var message = notification.Message.Data.EventBody.MessageBody.Content;

        if (message?.StartsWith(HostEnvs.CommandPrefix) is false)
        {
            return;
        }

        var cmdComponents = message!.Split(" ");
        var cmd = cmdComponents[0][1..];
        var args = cmdComponents[1..];

        var cmdInfo = _pluginManager.GetCommandManifests().FirstOrDefault(x => x.Name == cmd);
        if (cmdInfo is null)
        {
            _channelLogger.LogWarning($"指令不存在：`{message}`，" +
                                      $"发送者：<@!{notification.Message.Data.EventBody.DodoId}>，" +
                                      $"频道：<#{notification.Message.Data.EventBody.ChannelId}>，" +
                                      $"消息 ID：`{notification.Message.Data.EventBody.MessageId}`");
            return;
        }
        
        var senderDodoId = notification.Message.Data.EventBody.DodoId;
        var comeFrom = notification.Message.Data.EventBody.IslandId;
        var senderRoles = await GetMemberRole(senderDodoId, comeFrom);

        var memberInfo = new DodoMemberInfo
        {
            PersonalNickName = notification.Message.Data.EventBody.Personal.NickName,
            MemberNickName = notification.Message.Data.EventBody.Member.NickName,
            Level = notification.Message.Data.EventBody.Member.Level,
            Id = notification.Message.Data.EventBody.DodoId,
            JoinTime = DateTimeOffset.Parse(notification.Message.Data.EventBody.Member.JoinTime),
            IslandId = notification.Message.Data.EventBody.IslandId,
            Roles = senderRoles.Select(x =>
                new DodoMemberRole
                {
                    Id = x.RoleId,
                    Name = x.RoleName,
                    Color = x.RoleColor,
                    Position = x.Position,
                    Permission = Convert.ToInt32(x.Permission, 16)
                }).ToList()
        };

        var messageInfo = new DodoMessageInfo
        {
            MessageId = notification.Message.Data.EventBody.MessageId,
            ChannelId = notification.Message.Data.EventBody.ChannelId,
            EventId = notification.Message.Data.EventId,
            OriginalText = notification.Message.Data.EventBody.MessageBody.Content
        };

        var result = await cmdInfo.CommandExecutor.Execute(args, memberInfo, messageInfo, _provider, IsSuperAdmin(memberInfo));
        if (result == CommandExecutionResult.Unauthorized)
        {
            _channelLogger.LogWarning($"无权访问：`{message}`，" +
                                      $"发送者：<@!{notification.Message.Data.EventBody.DodoId}>，" +
                                      $"频道：<#{notification.Message.Data.EventBody.ChannelId}>，" +
                                      $"消息 ID：`{notification.Message.Data.EventBody.MessageId}`");
        }
    }
    
    private async Task<List<GetMemberRoleListOutput>> GetMemberRole(string dodoId, string islandId)
    {
        var cached = await _redis.StringGetAsync(new RedisKey($"member.role.list.{islandId}.{dodoId}"));
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<List<GetMemberRoleListOutput>>(cached.ToString())!;
        }

        var senderRoles = await _apiService.GetMemberRoleListAsync(new GetMemberRoleListInput
        {
            DodoId = dodoId, IslandId = islandId
        });

        var str = JsonSerializer.Serialize(senderRoles);
        
        await _redis.StringSetAsync(
            new RedisKey($"member.role.list.{islandId}.{dodoId}"),
            new RedisValue(str),
            TimeSpan.FromMinutes(10));

        return senderRoles;
    }

    private static bool IsSuperAdmin(DodoMemberInfo memberInfo)
    {
        return memberInfo.Roles.Any(x => (x.Permission >> 3) % 2 == 1);
    }
}
