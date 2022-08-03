// This file is a part of EeroBot project.
// EeroBot belongs to the NibiruResearchCenter.
// Licensed under the AGPL-3.0 license.

namespace DodoHosted.Base.Models;

/// <summary>
/// 消息模型
/// </summary>
public record DodoMessageInfo
{
    /// <summary>
    /// 消息 ID
    /// </summary>
    public required string MessageId { get; init; }
    
    /// <summary>
    /// 频道 ID
    /// </summary>
    public required string ChannelId { get; init; }
    
    /// <summary>
    /// 事件 ID
    /// </summary>
    public required string EventId { get; init; }
    
    /// <summary>
    /// 消息原文
    /// </summary>
    public required string OriginalText { get; init; }
}
