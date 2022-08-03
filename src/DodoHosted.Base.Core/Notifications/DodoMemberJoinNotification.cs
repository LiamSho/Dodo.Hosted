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

using DoDo.Open.Sdk.Models.Events;
using MediatR;

namespace DodoHosted.Base.Core.Notifications;

/// <summary>
/// 频道成员进入 Notification
/// </summary>
/// <param name="Message">Event 消息体</param>
public record DodoMemberJoinNotification(EventSubjectOutput<EventSubjectDataBusiness<EventBodyMemberJoin>> Message)
    : INotification;
