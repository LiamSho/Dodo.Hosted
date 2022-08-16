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

namespace DodoHosted.Open.Plugin;

/// <summary>
///     指令信息
/// </summary>
public sealed record CommandMetadata(string CommandName, string Description, string HelpText, Dictionary<string, string>? PermissionNodes = null)
{
    /// <summary>
    ///     指令名称，执行器将会匹配 `{PREFIX}CommandName` 来进行指令的执行。例如，该值为 <c>help</c>，
    ///     则将会执行 <c>{PREFIX}help</c> 的指令。请注意，该值不能为空，或者包含空格
    /// </summary>
    public string CommandName { get; init; } = CommandName;

    /// <summary>
    ///     指令简介
    /// </summary>
    public string Description { get; init; } = Description;

    /// <summary>
    ///     指令帮助文本，使用 <c>{{PREFIX}}</c> 来代替指令前缀，在使用 `{{PREFIX}}help [Command]` 后
    ///     将会输出该文本，只需在此处描述指令与子指令用途即可
    /// </summary>
    /// <remarks>
    /// 指令帮助文本输出模版：
    /// <para>指令 `{Command}` 的帮助描述：</para>
    /// <para>// 空行 * 1</para>
    /// <para>简介：{Description}</para>
    /// <para>// 空行 * 1</para>
    /// <para>{HelpText}</para>
    /// </remarks>
    public string HelpText { get; init; } = HelpText;

    /// <summary>
    ///     权限节点与说明列表，可以不定义，这个值只会用于用户查看所有权限时进行显示，对于指令的执行没有影响，
    ///     字典的 Key 为权限节点，Value 为说明
    /// </summary>
    public Dictionary<string, string> PermissionNodes { get; init; } = PermissionNodes ?? new Dictionary<string, string>();
}
