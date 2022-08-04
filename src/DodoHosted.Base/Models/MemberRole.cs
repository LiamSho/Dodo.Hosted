// This file is a part of EeroBot project.
// EeroBot belongs to the NibiruResearchCenter.
// Licensed under the AGPL-3.0 license.

namespace DodoHosted.Base.Models;

/// <summary>
/// 用户身份组
/// </summary>
public record MemberRole
{
    /// <summary>
    /// 身份组名称
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// 身份组 ID
    /// </summary>
    public required string Id { get; init; }
    
    /// <summary>
    /// 身份组色彩
    /// </summary>
    public required string Color { get; init; }
    
    /// <summary>
    /// 身份组位置
    /// </summary>
    public required int Position { get; init; }
    
    /// <summary>
    /// 身份组权限
    /// </summary>
    public required int Permission { get; init; }
}
