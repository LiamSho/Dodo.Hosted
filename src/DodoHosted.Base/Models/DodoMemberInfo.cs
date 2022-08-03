// This file is a part of EeroBot project.
// EeroBot belongs to the NibiruResearchCenter.
// Licensed under the AGPL-3.0 license.

namespace DodoHosted.Base.Models;

/// <summary>
/// 用户信息
/// </summary>
public record DodoMemberInfo
{
    /// <summary>
    /// 个人用户名
    /// </summary>
    public required string PersonalNickName { get; init; }
    
    /// <summary>
    /// 群昵称
    /// </summary>
    public required string MemberNickName { get; init; }
    
    /// <summary>
    /// 加入时间
    /// </summary>
    public required DateTimeOffset JoinTime { get; init; }
    
    /// <summary>
    /// 等级
    /// </summary>
    public required int Level { get; init; }
    
    /// <summary>
    /// 用户 ID
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// 群 ID
    /// </summary>
    public required string IslandId { get; init; }
    
    /// <summary>
    /// 所有身份组
    /// </summary>
    public required List<DodoMemberRole> Roles { get; init; }
}
