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

using DodoHosted.Base.App.Models;

namespace DodoHosted.Open.Plugin;

/// <summary>
/// Command 执行器接口
/// </summary>
/// <remarks>
/// 该接口实现类需要有 <see cref="CommandExecutorAttribute"/> 标签
/// </remarks>
/// <example>
/// <code>
/// // 此处使用了 C# 11 的新特性
/// [CommandExecutor(
///     commandName: "myCommand",
///     description: "指令说明",
///     helpText: @"""
/// - `{{PREFIX}}myCommand`    指令帮助文本 1
/// - `{{PREFIX}}myCommand do`    指令帮助文本 2
/// """)]
/// public class MyCommandExecutor : ICommandExecutor
/// {
///     public async Task Execute(string[] args, DodoMemberInfo sender, DodoMessageInfo message, bool shouldAllow = false)
///     {
///         if (shouldAllow is false)
///         {
///             // Perform permission check
///             // Return if permission check failed
///         }
///
///         // Do your job here
///     }
/// }
/// </code>
/// </example>
public interface ICommandExecutor
{
    /// <summary>
    /// 执行指令
    /// </summary>
    /// <param name="args">指令参数，为用户输入内容使用空格进行分离，然后去除第一项指令名称的余下部分</param>
    /// <param name="message">指令消息</param>
    /// <param name="provider">用于访问 DI 容器的 ServiceProvider，对于每次请求，都会使用一个新的 Scope</param>
    /// <param name="reply">回复发送者的消息</param>
    /// <param name="shouldAllow">
    /// 是否应当允许执行，大多指令都应有对应的权限检查，此参数为 True 表示指令消息的发送者拥有
    /// 群主或超级管理员权限，应当直接跳过权限检查，不过你仍然可以忽略此参数
    /// </param>
    /// <returns></returns>
    Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage message,
        IServiceProvider provider,
        Func<string, Task<string>> reply,
        bool shouldAllow = false);
}
