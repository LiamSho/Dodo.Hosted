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
// using DodoHosted.Base.Card;
// using DodoHosted.Base.Card.CardComponent;
// using DodoHosted.Base.Card.Enums;
// using DodoHosted.Base.Types;
// using DodoHosted.Lib.Plugin.Models;
//
// namespace DodoHosted.Lib.Plugin.Helper;
//
// public static class HelpMessageCard
// {
//     public static CardMessage GetCommandHelpMessage(this CommandManifest manifest)
//     {
//         var card = new CardMessage
//         {
//             Content = string.Empty,
//             Card = new Card
//             {
//                 Title = "指令帮助",
//                 Theme = CardTheme.Default,
//                 Components = new List<ICardComponent>
//                 {
//                     new Header($"{manifest.RootNode.Value} - {manifest.RootNode.Description}"),
//                 }
//             }
//         };
//         
//         var methods = manifest.Methods;
//         var rootCommand = methods.FirstOrDefault(x => x.Path.Length == 0);
//
//         if (rootCommand is not null)
//         {
//             card.Card.Components.Add(new TextFiled($"权限：`{GetPermissionText(rootCommand.PermissionNode)}`"));
//
//             if (rootCommand.Options.Count != 0)
//             {
//                 card.Card.Components.Add(new Divider());
//             
//                 foreach (var (_, (type, cmdOption)) in rootCommand.Options)
//                 {
//                     var title = $"--{cmdOption.Name}";
//                     if (string.IsNullOrEmpty(cmdOption.Abbr) is false)
//                     {
//                         title += $" | -{cmdOption.Abbr}";
//                     }
//
//                     var attrs = new List<string>
//                     {
//                         cmdOption.Required ? "`必填`" : "`可选`",
//                         $"`类型：{type.GetFriendlyName()}`"
//                     };
//                     if (cmdOption.Default is not null)
//                     {
//                         attrs.Add($"`默认值：{cmdOption.Default}`");
//                     }
//
//                     card.Card.Components.Add(new Header(title));
//                     card.Card.Components.Add(new TextFiled(string.Join(" ", attrs)));
//                     card.Card.Components.Add(new TextFiled(cmdOption.HelpText));
//                 }
//             }
//         }
//
//         var nodeCommands = manifest.Methods
//             .SkipWhile(x => x.Path.Length == 0)
//             .ToArray();
//         if (nodeCommands.Length == 0)
//         {
//             return card;
//         }
//
//         card.Card.Components.Add(new Divider());
//         foreach (var node in nodeCommands)
//         {
//             card.Card.Components.Add(new Header(string.Join(" ", node.Path)));
//             card.Card.Components.Add(new TextFiled($"权限：`{GetPermissionText(GetPermissionText(node.PermissionNode))}`"));
//             card.Card.Components.Add(new TextFiled(node.Description));
//         }
//         
//         return card;
//     }
//
//     private static string GetPermissionText(string permNode)
//     {
//         return string.IsNullOrEmpty(permNode)
//             ? "*"
//             : permNode;
//     }
// }
