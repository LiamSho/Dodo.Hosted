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

namespace DodoHosted.Base.Models;

/// <summary>
/// 指令消息
/// </summary>
public record CommandMessage
{
    /// <summary>
    /// 群 ID
    /// </summary>
    public required string IslandId { get; init; }

    /// <summary>
    /// 消息 ID
    /// </summary>
    public required string MessageId { get; init; }
    
    /// <summary>
    /// 频道 ID
    /// </summary>
    public required string ChannelId { get; init; }
    
    /// <summary>
    /// 用户 ID
    /// </summary>
    public required string MemberId { get; init; }
    
    /// <summary>
    /// 事件 ID
    /// </summary>
    public required string EventId { get; init; }
    
    /// <summary>
    /// 消息原文
    /// </summary>
    public required string OriginalText { get; init; }
    
    /// <summary>
    /// 用户昵称
    /// </summary>
    public required string PersonalNickname { get; init; }
    
    /// <summary>
    /// 用户群组昵称
    /// </summary>
    public required string MemberNickname { get; init; }

    /// <summary>
    /// 用户等级
    /// </summary>
    public required int MemberLevel { get; init; }
   
    /// <summary>
    /// 用户加入时间
    /// </summary>
    public required DateTimeOffset MemberJoinTime { get; init; }
    
    /// <summary>
    /// 所有身份组
    /// </summary>
    public List<MemberRole> Roles { get; set; } = new();
}
