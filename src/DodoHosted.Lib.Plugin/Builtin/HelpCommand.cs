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
// using DodoHosted.Open.Plugin;
// using DodoHosted.Open.Plugin.Attributes;
// using Microsoft.Extensions.DependencyInjection;
//
// namespace DodoHosted.Lib.Plugin.Builtin;
//
// #pragma warning disable CA1822
//
// [Cmd("help", "查看指令帮助")]
// public class HelpCommand : ICommandExecutor
// {
//     [CmdRunner("system.help", "查看指令帮助")]
//     public async Task<bool> GetAllCommands(
//         PluginBase.Context context,
//         [CmdOption("command", "c", false, "指令的名称，为空时显示所有可用指令", defaultValue: "")] string commandName,
//         [CmdOption("path", "p", false, "指令的路径，使用 `,` 分隔，为空时显示所有可用指令", defaultValue: "")] string commandPath)
//     {
//         var pm = context.Provider.GetRequiredService<IPluginManager>();
//         var manifests = pm.GetCommandManifests();
//
//         var manifest = manifests.FirstOrDefault(x => x.CommandName == commandName);
//         if (manifest is null)
//         {
//             await context.Functions.Reply($"指令 `{commandName}` 不存在");
//             return false;
//         }
//
//         if (string.IsNullOrEmpty(commandPath) is false)
//         {
//             
//         }
//         
//         return true;
//     }
// }
