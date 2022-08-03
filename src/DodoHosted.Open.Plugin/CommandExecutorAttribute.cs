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
/// 标记 <see cref="ICommandExecutor"/> 用于处理某一种指令
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class CommandExecutorAttribute : Attribute
{
    public string CommandName { get; set; }
    public string Description { get; set; }
    public string HelpText { get; set; }
    
    /// <summary>
    /// 标记 <see cref="ICommandExecutor"/> 添加指令元数据
    /// </summary>
    /// <param name="commandName">指令名，执行的指令将会为 <c>{{PREFIX}}Command</c>，该项不应当为空，也不应当在其中出现空格</param>
    /// <param name="description">指令简介</param>
    /// <param name="helpText">
    /// 指令帮助文本，使用 <c>{{PREFIX}}</c> 来代替指令前缀，在使用 `{{PREFIX}}help [Command]` 后
    /// 将会输出该文本，只需在此处描述指令与子指令用途即可
    /// </param>
    /// <remarks>
    /// 指令帮助文本输出模版：
    /// <para>指令 `{Command}` 的帮助描述：</para>
    /// <para>// 空行 * 1</para>
    /// <para>简介：{Description}</para>
    /// <para>// 空行 * 1</para>
    /// <para>{HelpText}</para>
    /// </remarks>
    public CommandExecutorAttribute(string commandName, string description, string helpText)
    {
        CommandName = commandName;
        Description = description;
        HelpText = helpText;
    }
}
