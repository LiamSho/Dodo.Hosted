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

using DoDo.Open.Sdk.Services;
using DodoHosted.Base.Card;
using DodoHosted.Base.Enums;

namespace DodoHosted.Base;

public static class PluginBase
{
    public delegate Task<string> Reply(string content, bool privateMessage = false);
    public delegate Task<string> ReplyCard(CardMessage cardMessage, bool privateMessage = false);
    public delegate Task<bool> PermissionCheck(string node);

    public record Context(Functions Functions, UserInfo UserInfo, EventInfo EventInfo, OpenApiService OpenApiService, IServiceProvider Provider);

    public record Functions(Reply Reply, ReplyCard ReplyCard, PermissionCheck PermissionCheck);
    public record UserInfo(string NickName, string AvatarUrl, Sex Sex, string MemberNickName, int MemberLevel, DateTimeOffset JoinTime, string DodoId);
    public record EventInfo(string IslandId, string ChannelId, string MessageId, string EventId, long EventTimeStamp);
}
