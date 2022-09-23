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

using DodoHosted.Base.App.Attributes;
using DodoHosted.Base.App.Context;
using DodoHosted.Lib.Plugin.Cards;

namespace DodoHosted.Lib.Plugin.Builtin;

#pragma warning disable CA1822
// ReSharper disable MemberCanBeMadeStatic.Global
// ReSharper disable MemberCanBePrivate.Global

public sealed class HelpCommand : ICommandExecutor
{
    public async Task<bool> GetAllCommands(
        CommandContext context,
        [Inject] IPluginManager pluginManager,
        [CmdOption("name", "n", "指令的名称，为空时显示所有可用指令", false)] string? commandName,
        [CmdOption("path", "p", "指令的路径，使用 `,` 分隔，为空时显示所有可用指令", false)] string? commandPath)
    {
        if (commandName is null)
        {
            var allCommandHelpCard = await pluginManager.GetCommandNodes()
                .GetCommandListMessage(context.PermissionCheck);

            await context.ReplyCard.Invoke(allCommandHelpCard);
            return true;
        }

        var commandNode = pluginManager.GetCommandNode(commandName);
        if (commandNode is null)
        {
            await context.Reply.Invoke($"找不到名为 {commandName} 的指令");
            return false;
        }

        var node = commandPath is null
            ? commandNode
            : commandNode.GetNode(commandPath.Split(","));

        if (node is null)
        {
            await context.Reply.Invoke("找不到指定路径的指令");
            return false;
        }

        var commandNodeHelpCard = await node.GetCommandHelpMessage(
            context.PermissionCheck);
        await context.ReplyCard.Invoke(commandNodeHelpCard);
        
        return true;
    }

    public CommandTreeBuilder GetBuilder()
    {
        return new CommandTreeBuilder("help", "显示帮助信息", "system.help", method: GetAllCommands);
    }
}
