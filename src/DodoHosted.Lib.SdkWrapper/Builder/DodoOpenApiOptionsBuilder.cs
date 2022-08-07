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

using DoDo.Open.Sdk.Models;
using DoDo.Open.Sdk.Services;
using Microsoft.Extensions.Logging;

namespace DodoHosted.Lib.SdkWrapper.Builder;

/// <summary>
/// 构建 <see cref="OpenApiOptions"/>.
/// </summary>
public class DodoOpenApiOptionsBuilder
{
    private readonly OpenApiOptions _options;
    private bool _useLogger;
    private LogLevel _logLevel;

    public DodoOpenApiOptionsBuilder()
    {
        _options = new OpenApiOptions();
        _logLevel = LogLevel.Information;
        _useLogger = true;
    }

    /// <summary>
    /// 设置 <see cref="OpenApiOptions.BaseApi"/>
    /// </summary>
    /// <param name="baseApi">BaseApi URL.</param>
    /// <returns></returns>
    public DodoOpenApiOptionsBuilder UseBaseApi(string baseApi)
    {
        _options.BaseApi = baseApi;
        return this;
    }

    /// <summary>
    /// 设置 <see cref="OpenApiOptions.ClientId"/>
    /// </summary>
    /// <param name="botId"></param>
    /// <returns></returns>
    public DodoOpenApiOptionsBuilder UseBotId(string botId)
    {
        _options.ClientId = botId;
        return this;
    }

    /// <summary>
    /// 设置 <see cref="OpenApiOptions.Token"/>
    /// </summary>
    /// <param name="botToken"></param>
    /// <returns></returns>
    public DodoOpenApiOptionsBuilder UseBotToken(string botToken)
    {
        _options.Token = botToken;
        return this;
    }

    /// <summary>
    /// 设置日志等级并开启日志记录
    /// </summary>
    /// <param name="logLevel">日志记录等级，默认为 <see cref="LogLevel.Information"/></param>
    /// <returns></returns>
    public DodoOpenApiOptionsBuilder UseLogger(LogLevel logLevel = LogLevel.Information)
    {
        _logLevel = logLevel;
        _useLogger = true;
        return this;
    }

    /// <summary>
    /// 关闭日志记录
    /// </summary>
    /// <returns></returns>
    public DodoOpenApiOptionsBuilder DisableLogger()
    {
        _useLogger = false;
        return this;
    }

    /// <summary>
    /// 设置 <see cref="OpenApiOptions.Log"/>
    /// </summary>
    /// <param name="logger">Logger 委托</param>
    /// <returns></returns>
    internal DodoOpenApiOptionsBuilder UseLogger(ILogger<OpenApiService> logger)
    {
        _options.Log = _useLogger ? s => logger.Log(_logLevel, "DodoOpenApiService: {DodoOpenApiMessage}", s) : null;
        return this;
    }

    /// <summary>
    /// 构建 <see cref="OpenApiOptions"/>
    /// </summary>
    /// <returns></returns>
    /// <exception cref="ArgumentException">
    /// <see cref="OpenApiOptions.ClientId"/> 或 <see cref="OpenApiOptions.Token"/> 为空
    /// </exception>
    internal OpenApiOptions Build()
    {
        if (string.IsNullOrEmpty(_options.ClientId) ||
            string.IsNullOrEmpty(_options.Token) ||
            string.IsNullOrEmpty(_options.BaseApi)) 
        {
            throw new ArgumentException("ClientId 或 Token 或 BaseApi 不可为空");
        }
        
        return _options;
    }
}
