# 插件系统

DodoHosted 可以通过插件系统来扩展自己的功能，插件是一个独立的 .NET 项目，需要使用 .NET 7.0 进行编写，并引用 [DodoHosted.Open.Plugin](https://www.nuget.org/packages/DodoHosted.Open.Plugin) 包。

## 编写插件

### 项目创建

创建一个新的 .NET 7.0 类库项目，并添加 Nuget 包依赖。

``` shell
dotnet new classlib -o MyPlugin -n MyPlugin -f net7.0
cd MyPlugin
dotnet add package DodoHosted.Open.Plugin
```

编辑 `MyPlugin.csproj` 文件，在 PackageReference 的末尾加上 `PrivateAssets="All"`，例如：

``` xml
<PackageReference Include="DodoHosted.Open.Plugin" Version="1.2.0" PrivateAssets="All" />
```

请注意，如果你还需要引入其他的 Nuget 包，请不要加上 `PrivateAssets="All"`

> 在载入插件时，每个插件的程序集会在单独的 LoadContext 中加载，因此，若 A 插件以来 C 包的 1.0.0 版本，而 B 插件依赖 C 包的 2.0.0 版本，也不会产生冲突问题。

### 插件元数据

新建一个 `plugin.json` 文件，写入插件元数据，例如：

``` json
{
  "$schema": "https://raw.githubusercontent.com/LiamSho/Dodo.Hosted/main/plugin-info-schema.json",
  "identifier": "my-plugin",
  "name": "MyPlugin",
  "version": "1.0.0",
  "description": "示例插件",
  "author": "Liam Sho",
  "entry_assembly": "MyPlugin"
}
```

你可以引入 Json Schema 来提供 Json 的自动补全和格式校验。其中，`entry_assembly` 是插件的入口程序集，一般来说为插件类库名称。

该文件需要随插件程序集一起发布，因此，可以修改 `MyPlugin.csproj` 文件，使其在编译时自动复制到输出路径。

``` xml
<ItemGroup>
    <None Update="plugin.json">
        <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
</ItemGroup>
```

### 插件生命周期

创建一个类，继承 `DodoHosted.Open.Plugin.IPluginLifetime` 接口，并实现其中的方法。

``` csharp
using DodoHosted.Open.Plugin;
using Microsoft.Extensions.Logging;

namespace MyPlugin;

public class Plugin : IPluginLifetime
{
    public Task Load(ILogger logger)
    {
        return Task.CompletedTask;
    }

    public Task Unload(ILogger logger)
    {
        return Task.CompletedTask;
    }
}
```

此处，Load 将在插件载入时运行，Unload 将在插件卸载前运行。

每个插件中必须要有且只能有一个实现了 `IPluginLifetime` 接口的类。

### 添加事件处理器

事件处理器是继承了 `IDodoHostedPluginEventHandler<T>` 接口的类，并实现其中的方法。

每个插件中可以有任意个事件处理器。

``` csharp
public class TextMessageListener : IDodoHostedPluginEventHandler<DodoChannelMessageEvent<MessageBodyText>>
{
    public Task Handle(DodoChannelMessageEvent<MessageBodyText> @event, IServiceProvider provider, ILogger logger)
    {
        logger.LogInformation("Received message: [{Channel}] {Sender}: {Message}",
            @event.Message.Data.EventBody.ChannelId,
            @event.Message.Data.EventBody.Member.NickName,
            @event.Message.Data.EventBody.MessageBody.Content);
        
        return Task.CompletedTask;
    }
}
```

### 添加命令处理器

命令处理器是继承了 `ICommandExecutor` 接口的类，并实现其中的方法。

DodoHosted 自身实现了 4 个命令处理器，你可以参考 [这些实现](https://github.com/LiamSho/Dodo.Hosted/tree/main/src/DodoHosted.Lib.Plugin/Builtin) 来编写你的命令处理器。

每个插件中可以有任意个事件处理器。

## 插件的编译和打包

### 编译发布插件

```
dotnet publish -c Release -o ./bin/publish
```

### 打包插件

插件需要打包为一个 Zip 文件，并且，`plugin.json` 文件需要放在根目录下。

如果你使用 PowerShell，可以使用以下命令：

``` PowerShell
Compress-Archive -Path ./bin/publish/* -Destination "./my-plugin.zip"
```

## 安装插件

只需要将插件打包的 Zip 文件放置在 DodoHosted 的插件目录下即可，DodoHosted 启动时将会载入所有插件，或者，你也不需要重新启动，只需要在管理员群组中使用指令 `{前缀}system plugin load <插件压缩包名称>` 即可直接载入插件。
