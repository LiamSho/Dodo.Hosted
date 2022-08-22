// This file is a part of EeroBot project.
// EeroBot belongs to the NibiruResearchCenter.
// Licensed under the AGPL-3.0 license.

using System.Text;
using DodoHosted.Base;
using DodoHosted.Base.App;
using DodoHosted.Base.App.Interfaces;
using DodoHosted.Base.App.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Lib.Plugin.Builtin;

public class HelpCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage cmdMessage,
        IServiceProvider provider,
        IPermissionManager permissionManager,
        PluginBase.Reply reply,
        bool shouldAllow = false)
    {
        if (shouldAllow is false)
        {
            if (await permissionManager.CheckPermission("system.command.help", cmdMessage) is false)
            {
                return CommandExecutionResult.Unauthorized;
            }
        }

        var pluginManager = provider.GetRequiredService<IPluginManager>();
        
        var messageBuilder = new StringBuilder();
        var commands = pluginManager.GetCommandInfos();
        
        switch (args.Length)
        {
            case 1:
                {
                    messageBuilder.AppendLine("可用指令:");
                    messageBuilder.AppendLine();
                    foreach (var info in commands)
                    {
                        messageBuilder.AppendLine($"- `{HostEnvs.CommandPrefix}{info.Name}`  {info.Description}");
                    }

                    break;
                }
            case 2:
                {
                    var cmd = args[1];
                    var info = commands.FirstOrDefault(x => x.Name == cmd);
                    if (info is null)
                    {
                        messageBuilder.AppendLine($"指令 `{HostEnvs.CommandPrefix}{cmd}` 不存在，执行 `{HostEnvs.CommandPrefix}help` 查看所有指令");
                    }
                    else
                    {
                        messageBuilder.AppendLine($"指令 `{HostEnvs.CommandPrefix}{cmd}` 的帮助描述:");
                        messageBuilder.AppendLine($"***简介***：{info.Description}");
                        messageBuilder.AppendLine("***指令表***：");
                        messageBuilder.AppendLine(info.HelpText);
                        messageBuilder.AppendLine("***权限表***：");
                        messageBuilder.AppendLine(info.PermissionNodesText);
                    }

                    break;
                }
            default:
                return CommandExecutionResult.Unknown;
        }

        var msg = messageBuilder.ToString();

        await reply.Invoke(msg);

        return CommandExecutionResult.Success;
    }

    public CommandMetadata GetMetadata() => new CommandMetadata(
        CommandName: "help",
        Description: "查看指令帮助",
        HelpText: @"""
- `{{PREFIX}}help`    查询所有已注册指令
- `{{PREFIX}}help <command>`    查询 <command> 指令的帮助
""",
        PermissionNodes: new Dictionary<string, string>
        {
            {"system.command.help", "允许使用 `help` 指令"}
        });
}
