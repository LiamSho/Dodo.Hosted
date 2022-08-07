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

using System.Reflection;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Base.App;

/// <summary>
/// Host 变量
/// </summary>
public static class HostEnvs
{
    public static HostConfiguration Configuration { get; set; } = new();
    
    /// <summary>
    /// 插件载入缓存目录
    /// </summary>
    public static string PluginCacheDirectory => Configuration.PluginCacheDirectory ?? ReadEnvironmentVariable(
        "DODO_HOSTED_PLUGIN_CACHE_DIRECTORY",
        Path.Combine(AssemblyDirectory, "PluginCache"));

    /// <summary>
    /// 插件目录
    /// </summary>
    public static string PluginDirectory => Configuration.PluginDirectory ?? ReadEnvironmentVariable(
            "DODO_HOSTED_PLUGIN_DIRECTORY",
            Path.Combine(AssemblyDirectory, "Plugin"));
    
    /// <summary>
    /// MongoDb 连接字符串
    /// </summary>
    public static string MongoDbConnectionString => Configuration.MongoDbConnectionString ?? ReadEnvironmentVariable(
        "DODO_HOSTED_MONGO_CONNECTION_STRING",
        string.Empty);

    /// <summary>
    /// MongoDb 数据库名称
    /// </summary>
    public static string MongoDbDatabaseName => Configuration.MongoDbDatabaseName ?? ReadEnvironmentVariable(
        "DODO_HOSTED_MONGO_DATABASE_NAME",
        "dodo-hosted");
    
    /// <summary>
    /// Dodo 机器人 Client ID
    /// </summary>
    public static string DodoSdkBotClientId => ReadEnvironmentVariable(
        "DODO_SDK_BOT_CLIENT_ID",
        string.Empty);

    /// <summary>
    /// Dodo 机器人 Token
    /// </summary>
    public static string DodoSdkBotToken => ReadEnvironmentVariable(
        "DODO_SDK_BOT_TOKEN",
        string.Empty);
    
    /// <summary>
    /// Dodo SDK API 终结点
    /// </summary>
    public static string DodoSdkApiEndpoint => Configuration.DodoSdkApiEndpoint ?? ReadEnvironmentVariable(
        "DODO_SDK_API_ENDPOINT",
        "https://botopen.imdodo.com");

    /// <summary>
    /// 是否开启 Channel Logger
    /// </summary>
    public static bool DodoHostedChannelLogEnabled => Configuration.DodoHostedChannelLogEnabled ?? ReadEnvironmentVariable(
        "DODO_HOSTED_CHANNEL_LOG_ENABLED", "false") is "true" or "yes";

    /// <summary>
    /// Channel Logger 频道 ID
    /// </summary>
    public static string DodoHostedChannelLogChannelId => Configuration.DodoHostedChannelLogChannelId ?? ReadEnvironmentVariable(
        "DODO_HOSTED_CHANNEL_LOG_CHANNEL_ID", string.Empty);

    /// <summary>
    /// 指令前缀
    /// </summary>
    public static string CommandPrefix => Configuration.CommandPrefix ?? ReadEnvironmentVariable(
        "DODO_HOSTED_COMMAND_PREFIX", "!");

    /// <summary>
    /// 版本号
    /// </summary>
    public static string DodoHostedVersion => ReadEnvironmentVariable(
        "DODO_HOSTED_VERSION", "0.0.0-DEBUG-BUILD");

    /// <summary>
    /// 是否在容器中运行
    /// </summary>
    public static bool DodoHostedInContainer => ReadEnvironmentVariable(
        "DODO_HOSTED_RUNTIME_CONTAINER", "false") is "true";
    
    /// <summary>
    /// Dodo OpenApi 消息日志记录等级，默认为 Debug
    /// </summary>
    public static LogLevel DodoHostedOpenApiLogLevel =>
        ReadEnvironmentVariable("DODO_HOSTED_OPENAPI_LOG_LEVEL", "Debug") switch
        {
            "Trace" => LogLevel.Trace,
            "Debug" => LogLevel.Debug,
            "Information" => LogLevel.Information,
            "Warning" => LogLevel.Warning,
            "Error" => LogLevel.Error,
            "Critical" => LogLevel.Critical,
            _ => LogLevel.Debug
        };

    
    /// <summary>
    /// 入口 Assembly 目录
    /// </summary>
    private static string AssemblyDirectory => new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName;

    private static string ReadEnvironmentVariable(string key, string defaultValue) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))
            ? defaultValue
            : Environment.GetEnvironmentVariable(key)!;
}
