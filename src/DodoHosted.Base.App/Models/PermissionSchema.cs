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

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace DodoHosted.Base.App.Models;

/// <summary>
/// 权限组
/// </summary>
/// <remarks>
///     <para>
///         对于每一个权限，存在 <see cref="Node"/>，<see cref="Island"/>，<see cref="Channel"/>，<see cref="Role"/>
///      与 <see cref="Value"/> 五个值，在判定权限时，会寻找最为匹配的 <see cref="Island"/>，<see cref="Channel"/>
///      与 <see cref="Role"/> 三个值，其中要注意的是 <see cref="Channel"/> 与 <see cref="Role"/> 的判定关系
///     </para>
///     <para>
///         所有未定义的权限节点，默认为全局 Deny
///     </para>
///     <para>
///         假设在群组 123，要实现权限 xxx 在 555 频道对有 666 权限组的用户开放，则设置
///         <code>
///             { Node = "xxx", Island = "123", Channel = "555", Role = "666", Value = Allow }
///         </code>
///     </para>
///     <para>
///         假设在群组 123，要实现权限 xxx 在 555 频道对除了有 888 权限组的用户开放，则设置
///         <code>
///             { Node = "xxx", Island = "123", Channel = "555", Role = "*", Value = Allow }
///             { Node = "xxx", Island = "123", Channel = "555", Role = "888", Value = Deny }
///         </code>
///     </para>
///     <para>
///         假设在群组 123，要实现权限 xxx 在所有频道对所有权限组的用户开放，则设置
///         <code>
///             { Node = "xxx", Island = "123", Channel = "*", Role = "*", Value = Allow }
///         </code>
///     </para>
///     <para>
///         权限组的检查可以完全由插件定义，若用户为超级管理员，则 DodoHosted 会建议插件跳过权限检查，不过插件能够忽略该建议
///     </para>
/// </remarks>
public record PermissionSchema
{
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.CSharpLegacy)]
    public Guid Id { get; init; } = Guid.NewGuid();
    
    /// <summary>
    ///     权限节点，格式为 <c>xxx.yyy.zzz</c>，类似于 Minecraft Bukkit 权限结构
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <c>ALLOW xxx.yyy.*</c> 与 <c>DENY xxx.yyy.zzz</c> 将会判定为
    ///         <c>xxx.yyy.z</c> 为允许，<c>xxx.yyy.zzz</c> 为不允许
    ///     </para>
    ///     <para>
    ///         <c>DENY xxx.*</c> 与 <c>ALLOW xxx.yyy.zzz</c> 将会判定为
    ///         <c>xxx.yyy</c> 与 <c>xxx.yyy.z</c> 为允许，<c>xxx.yyy.zzz</c> 为不允许
    ///     </para>
    ///     <para>
    ///         即，先判定节点名相同的，若存在，则以此为准，若不存在，则按级别向上寻找具有通配符
    ///     的权限组，以第一个找到的为准，若不存在定义，则默认为禁止
    ///     </para>
    /// </remarks>
    public required string Node { get; set; }

    /// <summary>
    /// 生效群组 Id，为 <c>*</c> 表示对所有群组生效，判定时优先判定 Id 匹配的项，若该值为空，则
    /// <see cref="Channel"/> 与 <see cref="Role"/> 将被忽略
    /// </summary>
    public required string Island { get; set; }

    /// <summary>
    /// 生效频道 Id，为 <c>*</c> 表示对所有频道生效，判定时优先判定 Id 匹配的项
    /// </summary>
    public required string Channel { get; set; }
    
    /// <summary>
    /// 生效身份组 Id，为 <c>*</c> 表示对所有身份组的用户，包括没有身份组的用户生效
    /// </summary>
    public required string Role { get; set; }
    
    /// <summary>
    /// 权限判定方式，为 allow，或 deny，均为小写，若是这两个值以外的任何值，将会判定为 deny，但是您可以直接读取数据库
    /// 然后使用自己的方式进行权限判定
    /// </summary>
    public required string Value { get; set; }
}
