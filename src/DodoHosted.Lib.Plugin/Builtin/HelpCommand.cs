// // This file is a part of Dodo.Hosted project.
// // 
// // Copyright (C) 2022 LiamSho and all Contributors
// // 
// // This program is free software: you can redistribute it and/or modify
// // it under the terms of the GNU Affero General Public License as
// // published by the Free Software Foundation, either version 3 of the
// // License, or (at your option) any later version.
// // 
// // This program is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY
//
// using DodoHosted.Base;
// using DodoHosted.Base.Command.Attributes;
// using DodoHosted.Base.Command.Builder;
// using DodoHosted.Open.Plugin;
//
// namespace DodoHosted.Lib.Plugin.Builtin;
//
// #pragma warning disable CA1822
// // ReSharper disable MemberCanBeMadeStatic.Global
// // ReSharper disable MemberCanBePrivate.Global
//
// public class HelpCommand : ICommandExecutor
// {
//     public async Task<bool> GetAllCommands(
//         PluginBase.Context context,
//         [CmdOption("command", "c", "指令的名称，为空时显示所有可用指令", false)] string? commandName,
//         [CmdOption("path", "p", "指令的路径，使用 `,` 分隔，为空时显示所有可用指令", false)] int? commandPath)
//     {
//         var v = commandPath?.ToString() ?? "NULL";
//         var k = commandName ?? "NULL";
//         await context.Functions.Reply.Invoke($"{k}, {v}");
//         
//         return true;
//     }
//
//     public async Task<bool> TestOne(PluginBase.Context context)
//     {
//         await context.Functions.Reply.Invoke("Test One");
//         return true;
//     }
//     
//     public async Task<bool> TestTwo(PluginBase.Context context)
//     {
//         await context.Functions.Reply.Invoke("Test Two");
//         return true;
//     }
//
//     public CommandTreeBuilder GetBuilder()
//     {
//         return new CommandTreeBuilder("help", "显示帮助信息", "system.command.help", method: GetAllCommands)
//             .Then("test-one", "测试指令一", "test", method: TestOne)
//             .Then("test", "测试指令二 ROOT","test", builder: x => x.
//                 Then("test-two", "测试指令二", string.Empty, method: TestTwo));
//     }
// }
