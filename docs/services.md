# DodoHosted 可注入服务

本文档包含 DodoHosted 可注入服务的介绍。

在某些地方，可以使用 `[Inject]` Attribute 来注入服务。

## 关于 InjectAttribute

`DodoHosted.Base.App.Attributes.InjectAttribute` 是一个标记性 Attribute，用于标记需要注入的服务。

在部分方法或构造函数内，可以使用这个标签标记参数，表示该参数是可注入服务。

这些可注入服务大部分来自于 DI 容器，但也有部分不是，不能通过 IServiceProvide 来获取。

## 可注入的服务

### IMongoDatabase

MongoDB 数据库，以单例方式注入。

若需使用 MongoDB，需要注意的是 Collection 名称不能以 `system` 开头，DodoHosted 保留所有 `system` 开头的 Collection 进行使用。

### IChannelLogger

频道日志记录器，用于将日志信息记录到 Dodo 群组的频道中，同时也会使用 `ILogger` 接口进行日志记录。

请注意，频道日志记录器是可以被频道管理员设置开关的。

### IPermissionManager

权限管理器，用于管理权限，提供了大量的方法用于权限检查、权限节点添加、权限节点删除等。

### OpenApiService

官方 SDK 的 OpenApiService，以单例方式注入 DI 容器。

### EventProcessService

事件处理器，用于处理 Dodo 群组的事件。

### ILogger<>

日志记录器，用于记录日志，泛型参数为当前类名。

### IMongoCollection<>

MongoDB Collection

该服务比较特殊，泛型参数为 Collection 的类型。该参数实际就是由 `IMongoDatabase.GetCollection<T>` 方法返回的结果，属于一种用于方便开发的服务。

在使用前，需要在插件实例类中重写 `RegisterMongoDbCollection` 方法，例如：

```csharp
public override Dictionary<Type, string> RegisterMongoDbCollection()
{
    return new Dictionary<Type, string>
    {
        { typeof(MyTypeOne), "plugin-id-my-type-one-collection" },
        { typeof(MyTypeTwo), "plugin-id-my-type-two-collection" },
        { typeof(BsonDocument), "plugin-id-bson-collection" }
    };
}
```

然后，在可以使用 `[Inject]` 的地方，就可以注入 `IMongoCollection<MyTypeOne>` 了，将会得到 `MyTypeOne` 类型的 Collection，其 Collection 名称为 `plugin-id-my-type-one-collection`。

这里需要注意的是，由于类型作为字典的 Key，所以不能有重复的类型，否则会抛出异常。例如，下面的示例是错误的：

```csharp
public override Dictionary<Type, string> RegisterMongoDbCollection()
{
    return new Dictionary<Type, string>
    {
        // 错误的，有两个相同的 Key
        { typeof(BsonDocument), "plugin-id-bson-collection" },
        { typeof(BsonDocument), "plugin-id-bson-another-collection" }
    };
}
```

由于注入服务时，使用的方式为 `[Inject] IMongoCollection<BsonDocument> collection`，DodoHosted 通过泛型参数来识别应当注入的 Collection，因此使用 Type 作为字典的 Key 防止重复。

### PluginConfigurationManager

插件配置管理器，用于管理插件配置。

插件配置管理器在一个预定义的 MongoDb Collection 中。请注意，PluginConfigurationManager 只用于存储插件的配置，不应用于存储每个频道的配置。

每个插件的配置存储在一个 MongoDb Document 中，因此有 4KB 的大小限制。
