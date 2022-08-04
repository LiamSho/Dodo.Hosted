// This file is a part of EeroBot project.
// EeroBot belongs to the NibiruResearchCenter.
// Licensed under the AGPL-3.0 license.

using System.Text;
using DodoHosted.Base;
using DodoHosted.Base.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.DependencyInjection;

namespace DodoHosted.Lib.Plugin.Builtin;

[CommandExecutor(
    commandName: "help",
    description: "查看指令帮助",
    helpText: @"""
- `{{PREFIX}}help`    查询所有已注册指令
- `{{PREFIX}}help <command>`    查询 <command> 指令的帮助
""")]
public class HelpCommand : ICommandExecutor
{
    public async Task<CommandExecutionResult> Execute(
        string[] args,
        CommandMessage cmdMessage,
        IServiceProvider provider,
        Func<string, Task<string>> reply,
        bool shouldAllow = false)
    {
        if (shouldAllow is false)
        {
            return CommandExecutionResult.Unauthorized;
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
                        messageBuilder.AppendLine();
                        messageBuilder.AppendLine($"简介：{info.Description}");
                        messageBuilder.AppendLine();
                        messageBuilder.AppendLine(info.HelpText);
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
}