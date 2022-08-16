# 项目结构

| 项目名称                      | 描述                    |
|---------------------------|-----------------------|
| DodoHosted.App            | 预配置好的 Host Runner     |
| DodoHosted.App.Core       | 对所有启动 Host 所需服务和项目的引用 |
| DodoHosted.Base           | 包含重新封装过的 Event        |
| DodoHosted.Base.App       | 包含所有的基础服务接口和实现        |
| DodoHosted.Lib.Plugin     | 插件功能的实现库              |
| DodoHosted.Lib.SdkWrapper | 官方 SDK 的进一步封装         |
| DodoHosted.Open.Plugin    | 提供给插件使用的接口            |

## DodoHosted.App.Core

该项目包含了所有启动 Host 所需要的内容，引用了 `DodoHosted.Lib.Plugin` 与 `DodoHosted.Lib.SdkWrapper` 项目。

``` csharp
var builder = Host.CreateDefaultBuilder();
builder.ConfigureServices((_, services) =>
{
    services.AddDodoHostedServices();
});
var app = builder.Build();
await app.RunAsync();
```

以上代码将从环境变量读取机器人配置，当然，`AddDodoHostedServices()` 提供两个可选参数，可以对 `OpenApiOptions` 和 `OpenEventOptions` 进行配置。

``` csharp
builder.ConfigureServices((_, services) =>
{
    services.AddDodoHostedServices(
        openApiOptionsBuilder => openApiOptionsBuilder
            .UseLogger(LogLevel.Warning)
            .UseBotId("YOUR BOT ID")
            .UseBotToken("YOUR BOT TOKEN")
            .UseBaseApi("DODO BASE API"),
        openEventOptionsBuilder => openEventOptionsBuilder
            .UseAsync()
            .UseReconnect());
});
```

## DodoHosted.Base

包含重新封装过的 Events 和 Event 接口 `IDodoHostedEvent`，其他所有项目都直接或间接引用了该项目，该项目直接依赖官方 SDK 项目。

## DodoHosted.Base.App

包含所有的基础服务接口和实现，引用了 `DodoHosted.Base` 项目以及 MongoDB。

- `IChannelLogger` 与 `IPermissionManager` 服务的定于与实现
- `DodoTextHelper` 帮助类，用于拆解消息中的频道 ID 与成员 ID
- `HostEnvs` 包含所有的配置项目，默认将从环境变量读取配置，可以通过为 `HostEnvs.Configuration` 静态属性赋值来覆盖默认从环境变量读取的配置

## DodoHosted.Lib.Plugin

插件功能的实现库，引用了 `DodoHosted.Lib.SdkWrapper` 与 `DodoHosted.Open.Plugin` 项目。

该项目实现了插件的载入与卸载功能，以及运行插件事件监听器，插件指令处理器的执行逻辑。

同时，该项目实现了 4 个内置的指令。

## DodoHosted.Lib.SdkWrapper

官方 SDK 的进一步封装，引用了 `DodoHosted.Base` 项目。

如果你需要使用 .NET Generic Host 或者 .NET Web Host 运行机器人，可以直接引用这个项目。

向 DI 容器添加服务扩展方法实现：

```csharp
public static IServiceCollection AddDodoServices(
    this IServiceCollection serviceCollection,
    Action<DodoOpenApiOptionsBuilder> dodoOpenApiOptionsBuilder,
    Action<DodoOpenEventOptionsBuilder> dodoOpenEventOptionsBuilder,
    bool includeEventProcessor = true)
```

项目内默认实现了一个事件处理器，可以通过订阅 `DodoEventProcessor.DodoEvent` event 来获取所有的事件。

你也可以实现自己的事件处理器，只需要将其以 Singleton 的方式注册到 DI 容器中即可，然后在使用上面的方法添加其他服务时，将 `includeEventProcessor` 设置为 `false` 即可。

```csharp
serviceCollection.AddSingleton<EventProcessService, YourEventProcessor>();
```

## DodoHosted.Open.Plugin

插件的接口，引用了 `DodoHosted.Base.App` 项目。

插件主要由事件处理器和指令处理器组成。

关于插件的详情，请参考 [插件系统](./plugin-system.md)
