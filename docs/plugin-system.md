# 插件系统

DodoHosted 可以通过插件系统来扩展自己的功能，插件是一个独立的 .NET 项目，需要使用 .NET 7.0 进行编写，并引用 [DodoHosted.Open.Plugin](https://www.nuget.org/packages/DodoHosted.Open.Plugin) 包。

## 关于插件

DodoHosted 的插件指引用了 DodoHosted.Open.Sdk 包，并包含 `plugin.json` 插件元数据的压缩文件包。

插件是实际的业务逻辑执行器，DodoHosted 提供了 4 个接口用于功能拓展，1 个抽象类作为插件实例类。

## 创建插件项目

### 项目创建

创建一个新的 .NET 7.0 类库项目，并添加 Nuget 包依赖。

``` shell
dotnet new classlib -o MyPlugin -n MyPlugin -f net7.0
cd MyPlugin
dotnet add package DodoHosted.Open.Plugin
```

编辑 `MyPlugin.csproj` 文件，在 PackageReference 的末尾加上 `PrivateAssets="All"`，例如：

``` xml
<PackageReference Include="DodoHosted.Open.Plugin" Version="3.0.0" PrivateAssets="All" />
```

请注意，如果你还需要引入其他的 Nuget 包，请不要加上 `PrivateAssets="All"`

> 在载入插件时，每个插件的程序集会在单独的 LoadContext 中加载，因此，若 A 插件依赖 C 包的 1.0.0 版本，而 B 插件依赖 C 包的 2.0.0 版本，也不会产生冲突问题。

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
  "entry_assembly": "MyPlugin",
  "api_version": 1
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

## 接口实现

创建一个类，继承自各个来自于 `DodoHosted.Open.Plugin` 命名空间下的抽象类或接口，可以实现插件的各类功能。

⚠️ 请注意：实现类必须为公共类，且必须标记为 `sealed`

### 插件配置 DodoHostedPluginConfiguration

该抽象类的实现最多只能有 1 个。

用于部分行为配置，包含一个虚方法 `RegisterMongoDbCollection` 用于注册 MongoDB 集合，关于 MongoDB 集合，请参考 [可注入服务](./services.md) 文档

### 插件生命周期 DodoHostedPluginLifetime

该抽象类的实现最多只能有 1 个。

用于插件生命周期事件的处理，构造函数中可以使用 `[Inject]` 标签注入 [可注入服务](./services.md)。

### 指令处理器 ICommandExecutor

请参考 [指令系统](./command-system.md) 文档。

### 事件处理器 IEventHandler<T>

用于处理各类事件，类型 T 为 `DodoHosted.Base.Events` 命名空间下继承了 `IDodoHostedEvent` 接口的各个事件类。

在构造函数中，可以使用 `[Inject]` 标签注入 [可注入服务](./services.md)。

### 后台服务 IPluginHostedService

用于插件后台服务的运行，类似于 `Microsoft.Extension.Hosting.IHostedService` 接口。

在构造函数中，可以使用 `[Inject]` 标签注入 [可注入服务](./services.md)。

### Web 事件 IPluginWebHandler

请参考 [Web 事件](./web-event.md) 文档。

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
