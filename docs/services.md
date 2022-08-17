# DodoHosted DI 服务

本文档包含 DodoHosted 默认 DI 容器中所有可用服务的介绍。

对于插件而言，所有的服务可以通过 `IServiceProvider` 的 `GetRequiredService<T>()` 方法获取。

以下内容中，标题为该服务的获取类型，以及生命周期。

- `S`: Singleton
- `C`: Scoped
- `T`: Transient

## IMongoDatabase (S)

MongoDB 数据库，以单例方式注入。

若需使用 MongoDB，需要注意的是 Collection 名称不能以 `system` 开头，DodoHosted 保留所有 `system` 开头的 Collection 进行使用。

## IChannelLogger (C)

频道日志记录器，用于将日志信息记录到 Dodo 群组的频道中，同时也会使用 `ILogger` 接口进行日志记录。

请注意，频道日志记录器是可以被频道管理员设置开关的。

## IPermissionManager (C)

权限管理器，用于管理权限，提供了大量的方法用于权限检查、权限节点添加、权限节点删除等。

## IPluginManager (S)

插件管理器，用于管理插件，提供插件的加载、卸载方法，以及运行指令和事件的方法。

## OpenApiService (S)

官方 SDK 的 OpenApiService，以单例方式注入 DI 容器。
