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

namespace DodoHosted.Base;

/// <summary>
/// Host 变量
/// </summary>
public static class HostEnvs
{
    /// <summary>
    /// 插件载入缓存目录
    /// </summary>
    public static string PluginCacheDirectory => ReadEnvironmentVariable(
        "DODO_HOSTED_PLUGIN_CACHE_DIRECTORY",
        Path.Combine(AssemblyDirectory, "PluginCache"));

    /// <summary>
    /// 插件目录
    /// </summary>
    public static string PluginDirectory => ReadEnvironmentVariable(
            "DODO_HOSTED_PLUGIN_DIRECTORY",
            Path.Combine(AssemblyDirectory, "Plugin"));
    
    /// <summary>
    /// MongoDb 连接字符串
    /// </summary>
    public static string MongoDbConnectionString => ReadEnvironmentVariable(
        "DODO_HOSTED_MONGO_CONNECTION_STRING",
        string.Empty);

    /// <summary>
    /// MongoDb 数据库名称
    /// </summary>
    public static string MongoDbDatabaseName => ReadEnvironmentVariable(
        "DODO_HOSTED_MONGO_DATABASE_NAME",
        "dodo-hosted");
    
    /// <summary>
    /// Redis 连接配置字符串
    /// </summary>
    public static string RedisConnectionConfiguration => ReadEnvironmentVariable(
        "DODO_HOSTED_REDIS_CONNECTION_CONFIGURATION",
        string.Empty);

    /// <summary>
    /// Redis 数据库
    /// </summary>
    public static int RedisDatabaseId =>
        int.TryParse(ReadEnvironmentVariable("DODO_HOSTED_REDIS_DATABASE_ID", "-1"), out var index)
            ? index
            : -1;
    
    /// <summary>
    /// Dodo SDK API 终结点
    /// </summary>
    public static string DodoSdkApiEndpoint => ReadEnvironmentVariable(
        "DODO_SDK_API_ENDPOINT",
        "https://botopen.imdodo.com");

    /// <summary>
    /// 是否开启 Channel Logger
    /// </summary>
    public static readonly bool DodoHostedChannelLogEnabled = ReadEnvironmentVariable(
        "DODO_HOSTED_CHANNEL_LOG_ENABLED", "false") is "true" or "yes";

    /// <summary>
    /// Channel Logger 频道 ID
    /// </summary>
    public static readonly string DodoHostedChannelLogChannelId = ReadEnvironmentVariable(
        "DODO_HOSTED_CHANNEL_LOG_CHANNEL_ID", string.Empty);

    /// <summary>
    /// 指令前缀
    /// </summary>
    public static readonly string CommandPrefix = ReadEnvironmentVariable(
        "DODO_HOSTED_COMMAND_PREFIX", "!");
    
    /// <summary>
    /// 入口 Assembly 目录
    /// </summary>
    public static string AssemblyDirectory => new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName;

    private static string ReadEnvironmentVariable(string key, string defaultValue) =>
        string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key))
            ? defaultValue
            : Environment.GetEnvironmentVariable(key)!;
}
