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

using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using DoDo.Open.Sdk.Models.Channels;
using DoDo.Open.Sdk.Models.Members;
using DoDo.Open.Sdk.Models.Messages;
using DoDo.Open.Sdk.Services;
using DodoHosted.Base;
using DodoHosted.Base.Interfaces;
using DodoHosted.Base.Models;
using DodoHosted.Lib.Plugin.Exceptions;
using DodoHosted.Lib.Plugin.Models;
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace DodoHosted.Lib.Plugin;

/// <inheritdoc />
public class PluginManager : IPluginManager
{
    private readonly ILogger<PluginManager> _logger;
    private readonly IChannelLogger _channelLogger;
    private readonly IServiceProvider _provider;
    private readonly IDatabase _redis;
    private readonly OpenApiService _openApiService;

    private readonly ConcurrentDictionary<string, PluginManifest> _plugins;
    
    private readonly DirectoryInfo _pluginCacheDirectory;
    private readonly DirectoryInfo _pluginDirectory;

    private readonly CommandManifest[] _builtinCommands;

    private IEnumerable<CommandManifest> AllCommands => _plugins.IsEmpty
        ? _builtinCommands
        : _plugins.Values
            .Select(x => x.CommandManifests)
            .Aggregate((x, y) => x.Concat(y).ToArray())
            .Concat(_builtinCommands)
            .ToArray();

    public PluginManager(
        ILogger<PluginManager> logger,
        IChannelLogger channelLogger,
        IServiceProvider provider,
        IDatabase redis,
        OpenApiService openApiService)
    {
        _logger = logger;
        _channelLogger = channelLogger;
        _provider = provider;
        _redis = redis;
        _openApiService = openApiService;

        _pluginCacheDirectory = new DirectoryInfo(HostEnvs.PluginCacheDirectory);
        _pluginDirectory = new DirectoryInfo(HostEnvs.PluginDirectory);
        _plugins = new ConcurrentDictionary<string, PluginManifest>();

        _builtinCommands = FetchCommandExecutors(this.GetType().Assembly.GetTypes()).ToArray();
    }

    /// <inheritdoc />
    public PluginInfo[] GetLoadedPluginInfos()
    {
        return _plugins.Select(x => x.Value.PluginInfo).ToArray();
    }

    /// <inheritdoc />
    public CommandInfo[] GetCommandInfos()
    {
        var manifests = _plugins
            .Select(x => x.Value.CommandManifests)
            .ToArray();
        return (manifests.Length == 0
            ? _builtinCommands
            : manifests
                .Aggregate((x, y) => x.Concat(y).ToArray())
                .Concat(_builtinCommands))
            .Select(x => x as CommandInfo)
            .ToArray();
    }
    
    /// <inheritdoc />
    public async Task LoadPlugin(FileInfo bundle)
    {
        try
        {
            // 检查插件包是否存在
            if (bundle.Exists is false)
            {
                throw new FileNotFoundException("找不到插件包", bundle.Name);
            }

            // 读取和解析 plugin.json 文件
            await using var fs = bundle.OpenRead();
            using var zipArchive = new ZipArchive(fs, ZipArchiveMode.Read);
        
            var pluginInfoFileEntry = zipArchive.Entries.FirstOrDefault(x => x.Name == "plugin.json");

            if (pluginInfoFileEntry is null)
            {
                throw new InvalidPluginBundleException(bundle.Name, "找不到 plugin.json");
            }

            await using var pluginInfoReader = pluginInfoFileEntry.Open();

            var pluginInfo = await JsonSerializer.DeserializeAsync<PluginInfo>(pluginInfoReader);

            if (pluginInfo is null)
            {
                throw new InvalidPluginBundleException(bundle.Name, "无法解析 plugin.json");
            }
            _logger.LogTrace("已载入插件 {TracePluginBundleName} 信息 {TracePluginInfoDeserialized}", bundle.Name, pluginInfo);

            // 检查是否已有相同 Identifier 的插件
            var existingPlugin = _plugins.FirstOrDefault(x => x.Key == pluginInfo.Identifier).Value;
            if (existingPlugin is not null)
            {
                throw new PluginAlreadyLoadedException(existingPlugin.PluginInfo, pluginInfo);
            }
            _logger.LogTrace("未找到相同 Identifier 的插件 {TracePluginInfoIdentifier}", pluginInfo.Identifier);

            // 解压插件包
            var pluginCacheDirectoryPath = Path.Combine(_pluginCacheDirectory.FullName, pluginInfo.Identifier);
            var pluginCacheDirectory = new DirectoryInfo(pluginCacheDirectoryPath);

            if (pluginCacheDirectory.Exists)
            {
                pluginCacheDirectory.Delete(true);
                pluginCacheDirectory.Create();
                _logger.LogTrace("已删除已存在的插件缓存目录 {TracePluginCacheDirectoryPath}", pluginCacheDirectoryPath);
            }
            
            ZipFile.ExtractToDirectory(bundle.FullName, pluginCacheDirectory.FullName);
            _logger.LogTrace("已解压插件包 {TracePluginBundleName} 到 {TracePluginCacheDirectoryPath}", bundle.Name, pluginCacheDirectoryPath);

            // 载入程序集
            var entryAssembly = pluginCacheDirectory
                .GetFiles($"{pluginInfo.EntryAssembly}.dll", SearchOption.TopDirectoryOnly)
                .FirstOrDefault();

            if (entryAssembly is null)
            {
                throw new InvalidPluginBundleException(bundle.Name, $"找不到 {pluginInfo.EntryAssembly}.dll");
            }
            _logger.LogTrace("找到插件程序集 {TracePluginAssemblyPath}", entryAssembly.FullName);

            var context = new PluginAssemblyLoadContext(pluginCacheDirectory.FullName);
            var assembly = context.LoadFromAssemblyPath(entryAssembly.FullName);
            _logger.LogTrace("已载入插件程序集 {TracePluginAssemblyName}", assembly.FullName);

            // 载入事件处理器
            var pluginAssemblyTypes = assembly.GetTypes();

            var eventHandlers = FetchEventHandlers(pluginAssemblyTypes);
            
            // 载入指令处理器
            var commandExecutors = FetchCommandExecutors(pluginAssemblyTypes);
            
            // 添加插件
            var pluginManifest = new PluginManifest
            {
                PluginEntryAssembly = assembly,
                Context = context,
                PluginInfo = pluginInfo,
                EventHandlers = eventHandlers.ToArray(),
                CommandManifests = commandExecutors.ToArray()
            };
            var success = _plugins.TryAdd(pluginInfo.Identifier, pluginManifest);
            Debug.Assert(success);
            
            _logger.LogInformation("已载入插件 {PluginInfo}，事件处理器 {EventHandlerCount} 个，指令 {CommandCount} 个",
                pluginInfo, pluginManifest.EventHandlers.Length, pluginManifest.CommandManifests.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "插件包 {PluginBundleName} 载入失败，{ExceptionMessage}", bundle.Name, ex.Message);
        }
    }
    
    /// <inheritdoc />
    public async Task LoadPlugins()
    {
        var bundles = _pluginDirectory.GetFiles("*.zip", SearchOption.TopDirectoryOnly);

        foreach (var bundle in bundles)
        {
            await LoadPlugin(bundle);
        }
    }

    /// <inheritdoc />
    public bool UnloadPlugin(string pluginIdentifier)
    {
        _logger.LogInformation("执行卸载插件 {PluginUnloadIdentifier} 任务", pluginIdentifier);
        var _ = _plugins.TryRemove(pluginIdentifier, out var pluginManifest);
        pluginManifest?.Context.Unload();
        
        GC.Collect();
        
        var status = _plugins.ContainsKey(pluginIdentifier) is false;
        
        _logger.Log(status ? LogLevel.Information : LogLevel.Warning,
            "插件 {PluginUnloadIdentifier} 卸载任务完成，{PluginUnloadStatus}",
            pluginIdentifier, status ? "成功" : "失败");

        return status;
    }

    /// <inheritdoc />
    public void UnloadPlugins()
    {
        _logger.LogInformation("执行卸载所有插件任务");
        
        var pluginManifests = _plugins.Values.ToList();
        _plugins.Clear();

        foreach (var pluginManifest in pluginManifests)
        {
            pluginManifest.Context.Unload();
        }
        
        GC.Collect();
        
        _logger.LogInformation("卸载所有插件任务已完成，当前插件数量：{PluginsCount}", _plugins.Count);
    }

    /// <inheritdoc />
    public async Task RunCommand(CommandMessage cmdMessage)
    {
        var args = GetCommandArgs(cmdMessage.OriginalText).ToArray();
        _logger.LogTrace("已解析接收到的指令：{TraceReceivedCommandParsed}", $"[{string.Join(", ", args)}]");
        if (args.Length == 0)
        {
            return;
        }

        var command = args[0];
        var cmdInfo = AllCommands.FirstOrDefault(x => x.Name == command);
        var reply = async Task<string>(string s) =>
        {
            _logger.LogTrace("回复消息 {TraceReplyTargetId}", s);
            var output = await _openApiService.SetChannelMessageSendAsync(
                new SetChannelMessageSendInput<MessageBodyText>
                {
                    ChannelId = cmdMessage.ChannelId,
                    MessageBody = new MessageBodyText { Content = s, },
                    ReferencedMessageId = cmdMessage.MessageId
                });
            _logger.LogTrace("已回复消息, 消息 ID 为 {TraceReplyMessageId}", output.MessageId);
            
            return output.MessageId;
        };
        
        if (cmdInfo is null)
        {
            _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，发送者 {CommandSender}，频道 {CommandSendChannel}，消息 {CommandMessage}",
                cmdMessage.OriginalText, CommandExecutionResult.Unknown, $"{cmdMessage.PersonalNickname} ({cmdMessage.MemberId})", cmdMessage.ChannelId, cmdMessage.MessageId);
            _channelLogger.LogWarning($"指令不存在：`{cmdMessage.OriginalText}`，" +
                                      $"发送者：<@!{cmdMessage.MemberId}>，" +
                                      $"频道：<#{cmdMessage.ChannelId}>，" +
                                      $"消息 ID：`{cmdMessage.MessageId}`");
            await reply.Invoke($"指令 `{cmdMessage.OriginalText}` 不存在，执行 `{HostEnvs.CommandPrefix}help` 查看所有可用指令");
            return;
        }

        var senderRoles = await GetMemberRole(cmdMessage.MemberId, cmdMessage.IslandId);

        cmdMessage.Roles = senderRoles
            .Select(x =>
                new MemberRole
                {
                    Id = x.RoleId,
                    Name = x.RoleName,
                    Color = x.RoleColor,
                    Position = x.Position,
                    Permission = Convert.ToInt32(x.Permission, 16)
                })
            .ToList();
        
        var result = await cmdInfo.CommandExecutor.Execute(args, cmdMessage, _provider, reply,IsSuperAdmin(cmdMessage.Roles));
        _logger.LogTrace("指令执行结果：{TraceCommandExecutionResult}", result);

        switch (result)
        {
            case CommandExecutionResult.Success:
                break;
            case CommandExecutionResult.Failed:
                await reply.Invoke($"指令 `{cmdMessage.OriginalText}` 执行失败");
                break;
            case CommandExecutionResult.Unknown:
                await reply.Invoke($"指令 `{cmdMessage.OriginalText}` 不存在或存在格式错误\n\n" +
                                   $"指令 `{HostEnvs.CommandPrefix}{args[0]}` 的帮助描述：\n\n" +
                                   cmdInfo.HelpText);
                break;
            case CommandExecutionResult.Unauthorized:
                _channelLogger.LogWarning($"无权访问：`{cmdMessage.OriginalText}`，" +
                                          $"发送者：<@!{cmdMessage.MemberId}>，" +
                                          $"频道：<#{cmdMessage.ChannelId}>，" +
                                          $"消息 ID：`{cmdMessage.MessageId}`");
                break;
            default:
                _channelLogger.LogError($"未知的指令执行结果：`{result}`");
                break;
        }
        
        _logger.LogInformation("指令 {Command} 执行结果 {CommandExecutionResult}，发送者 {CommandSender}，频道 {CommandSendChannel}，消息 {CommandMessage}",
            cmdMessage.OriginalText, result, $"{cmdMessage.PersonalNickname} ({cmdMessage.MemberId})", cmdMessage.ChannelId, cmdMessage.MessageId);
    }

    /// <summary>
    /// 从 Plugin Assembly 中载入所有的事件处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns><see cref="EventHandlerManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private IEnumerable<EventHandlerManifest> FetchEventHandlers(IEnumerable<Type> types)
    {
        var eventHandlerTypes = types
            .Where(x => x != typeof(IDodoHostedPluginEventHandler<>))
            .Where(x => x
                .GetInterfaces()
                .Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IDodoHostedPluginEventHandler<>)))
            .Where(x => x.ContainsGenericParameters is false);

        var manifests = new List<EventHandlerManifest>();
        foreach (var type in eventHandlerTypes)
        {
            var interfaceType = type.GetInterfaces()
                .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDodoHostedPluginEventHandler<>));
                
            var handler = Activator.CreateInstance(type);
            var method = interfaceType.GetMethod("Handle");
            var eventType = interfaceType.GetGenericArguments().FirstOrDefault();

            if (handler is null)
            {
                throw new PluginAssemblyLoadException($"无法创建插件事件处理器 {type.FullName} 的实例");
            }
            if (method is null)
            {
                throw new PluginAssemblyLoadException($"找不到到插件事件处理器 {type.FullName} 的 Handle 方法");
            }
            if (eventType is null)
            {
                throw new PluginAssemblyLoadException($"找不到到插件事件处理器 {type.FullName} 的事件类型");
            }
            
            _logger.LogTrace("已载入事件处理器 {TraceLoadedEventHandler}", type.FullName);
            
            manifests.Add(new EventHandlerManifest
            {
                EventHandler = handler,
                EventType = eventType,
                EventHandlerType = type,
                HandlerMethod = method
            });
        }

        return manifests;
    }

    /// <summary>
    /// 从 Plugin Assembly 中载入所有的指令处理器
    /// </summary>
    /// <param name="types">Plugin Assembly 中所有的类型</param>
    /// <returns><see cref="CommandManifest"/> 清单</returns>
    /// <exception cref="PluginAssemblyLoadException">载入失败</exception>
    private IEnumerable<CommandManifest> FetchCommandExecutors(IEnumerable<Type> types)
    {
        var commandExecutorTypes = types
            .Where(x => x != typeof(ICommandExecutor))
            .Where(x => x.IsAssignableTo(typeof(ICommandExecutor)))
            .Where(x => x.ContainsGenericParameters is false)
            .ToList();
        
        var manifests = new List<CommandManifest>();
        foreach (var type in commandExecutorTypes)
        {
            var attribute = type.GetCustomAttribute<CommandExecutorAttribute>();
            if (attribute is null)
            {
                throw new PluginAssemblyLoadException($"找不到 {type.FullName} 的 {nameof(CommandExecutorAttribute)}");
            }

            var instance = Activator.CreateInstance(type);
            if (instance is null)
            {
                throw new PluginAssemblyLoadException($"无法创建指令处理器 {type.FullName} 的实例");
            }
            
            _logger.LogTrace("已载入指令处理器 {TraceLoadedCommandHandler}", type.FullName);
            
            manifests.Add(new CommandManifest
            {
                Name = attribute.CommandName,
                Description = attribute.Description,
                HelpText = FormatCommandHelpText(attribute.HelpText),
                CommandExecutor = (ICommandExecutor)instance
            });
        }

        return manifests;
    }
    
    /// <summary>
    /// 格式化指令帮助文档输出
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns>
    private static string FormatCommandHelpText(string message)
    {
        var msg = message.Replace("{{PREFIX}}", HostEnvs.CommandPrefix);

        while (msg.StartsWith("\"") || msg.StartsWith("\n"))
        {
            msg = msg[1..];
        }

        while (msg.EndsWith("\"") || msg.EndsWith("\n"))
        {
            msg = msg[..^1];
        }

        return msg;
    }

    /// <summary>
    /// 解析获取指令参数
    /// </summary>
    /// <param name="commandMessage"></param>
    /// <returns></returns>
    private static IEnumerable<string> GetCommandArgs(string commandMessage)
    {
        if (string.IsNullOrEmpty(commandMessage) || commandMessage.Length < 2)
        {
            return Array.Empty<string>();
        }
        
        var args = new List<string>();
        var command = commandMessage[1..].TrimEnd().AsSpan();
        var startPointer = 0;
    
        var inQuote = false;
            
        // /cmd "some thing \"in\" quote" value
        // cmd | some thing "in" quote | value
            
        for (var movePointer = 0; movePointer < command.Length; movePointer++)
        {
            if (command[movePointer] == '"')
            {
                if (movePointer == 0)
                {
                    return new[] { commandMessage[1..] };
                }
                    
                if (command[movePointer - 1] == '\\')
                {
                    continue;
                }
                    
                inQuote = !inQuote;
            }
                
            if (command[movePointer] != ' ')
            {
                continue;
            }
    
            if (inQuote)
            {
                continue;
            }
    
            if (command[movePointer - 1] == '"')
            {
                args.Add(command.Slice(startPointer + 1, movePointer - startPointer - 2)
                    .ToString()
                    .Replace("\\", string.Empty));
            }
            else
            {
                args.Add(command.Slice(startPointer, movePointer - startPointer).ToString());
            }
            startPointer = movePointer + 1;
        }
            
        args.Add(command[startPointer..].ToString());

        return args;
    }
    
    private async Task<List<GetMemberRoleListOutput>> GetMemberRole(string dodoId, string islandId)
    {
        var cached = await _redis.StringGetAsync(new RedisKey($"member.role.list.{islandId}.{dodoId}"));
        if (cached.HasValue)
        {
            return JsonSerializer.Deserialize<List<GetMemberRoleListOutput>>(cached.ToString())!;
        }

        var senderRoles = await _openApiService.GetMemberRoleListAsync(new GetMemberRoleListInput
        {
            DodoId = dodoId, IslandId = islandId
        });

        var str = JsonSerializer.Serialize(senderRoles);
        
        await _redis.StringSetAsync(
            new RedisKey($"member.role.list.{islandId}.{dodoId}"),
            new RedisValue(str),
            TimeSpan.FromMinutes(10));

        return senderRoles;
    }

    private static bool IsSuperAdmin(IEnumerable<MemberRole> roles)
    {
        return roles.Any(x => (x.Permission >> 3) % 2 == 1);
    }
}
