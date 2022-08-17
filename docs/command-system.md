# 指令系统

指令是 BOT 最常用到的功能，因此 DodoHosted 在频道文字消息的基础上，制作了指令系统。

指令系统主要由指令执行器和权限管理器组成。

## 指令

一条指令的样式如下：

``` txt
{{PREFIX}}island send #文字频道 "你好\n \"世界\""
```

其中，`{{PREFIX}}` 是单个字符，可以自定义配置，默认值为 `!`。

前缀后的第一个空格分隔指令的名称和参数表，后面的空格分隔各个参数。

如果有参数中需要包含空格，或者转义字符，可以使用双引号包裹参数。

在指令系统匹配到前缀后，将会对指令进行拆分，得到一个数组，拆分后的指令将会被传递给指令执行器。

例如，上面的指令将会被拆分为 `["island", "send", "<#频道的ID>", "你好\n \"世界\""]`。

## 指令执行器

`ICommandExecutor` 接口定义了指令执行器的接口，创建一个类，继承该接口，实现其中的方法，即可实现指令执行器。

指令执行器有两个需要实现的方法：

``` csharp
Task<CommandExecutionResult> Execute(
    string[] args,
    CommandMessage message,
    IServiceProvider provider,
    IPermissionManager permissionManager,
    Func<string, Task<string>> reply,
    bool shouldAllow = false);
```

第一个为指令的运行逻辑。在匹配到指令后，将会执行该方法，传入拆分后的指令数组，消息对象，ServiceProvider，权限管理器，消息回复委托，以及用户是否是超级管理员的标志。

``` csharp
CommandMetadata GetMetadata();
```

第二个方法用于获取指令的元数据。参考实现如：

``` csharp
public CommandMetadata GetMetadata() => new(
        CommandName: "help",
        Description: "查看指令帮助",
        HelpText: @"""
- `{{PREFIX}}help`    查询所有已注册指令
- `{{PREFIX}}help <command>`    查询 <command> 指令的帮助
""",
        PermissionNodes: new Dictionary<string, string>
        {
            {"system.command.help", "允许使用 `help` 指令"}
        });
```

`HelpText` 中的  `{{PREFIX}}` 将会被替换为实际的指令前缀。


## 权限管理器

DodoHosted 实现了一套类似于 Minecraft Bukkit 的权限系统，可以使用 `IPermissionManager` 进行权限的管理。

权限由权限节点、作用域、以及值组成，权限节点格式类似于：

``` txt
system.command.*
system.command.help
system.command.pm.add
your.own.permission.node
```

其中，`system.command.*` 将能够同时匹配 `system.command.help` 与 `system.command.pm.add`。

在插件中定义权限节点时，不可以使用通配符，通配符仅是用于设置用户权限使用的。其次，请勿使用以 `system` 开头的权限节点，这些权限节点是系统保留的。

作用域由频道和身份组共同定义，值可以是 `allow` 或者 `deny`。

假设某条指令需要 `my.command.hello` 权限，则在检查权限时，将会依次按照顺序寻找：

``` txt
my.command.hello
my.command.*
my.*
*
```

在找到有某个定义后，则会按照顺序匹配作用域：

``` txt
1. 频道 与 身份组 均匹配
2. 频道 匹配，身份组为通配符
3. 频道 为通配符，身份组 匹配
4. 频道 为通配符，身份组 为通配符
```

找到的第一个匹配项目就将会返回。

例如，有如下权限定义：

| 节点 | 频道 | 身份组 | 值 |
| ---- | ---- | ---- | ---- |
| my.command.hello | * | A | allow |
| my.command.hello | CB | A | deny |
| my.command.* | CA | * | allow |

1. 在频道 CA 中，身份组为 A 的用户使用指令

匹配 `my.command.hello`，找到了两个节点定义，按照顺序，匹配到频道为 `*`，身份组为 `A` 的定义，因此结果为 `allow`

2. 在频道 CB 中，身份组为 A 的用户使用指令

匹配 `my.command.hello`，找到了两个节点定义，按照顺序，匹配到频道为 `CB`，身份组为 `A` 的定义，因此结果为 `deny`

3. 在频道 CA 中，身份组为 B 的用户使用指令

匹配 `my.command.hello`，找到了两个节点定义，但是这两个节点均不匹配，因此再去寻找 `my.command.*` 的节点定义，共有一个，匹配到频道为 `CA`，身份组为 `*` 的定义，因此结果为 `allow`

4. 在频道 CB 中，身份组为 B 的用户使用指令

匹配 `my.command.hello`，找到了两个节点定义，但是这两个节点均不匹配，因此再去寻找 `my.command.*` 的节点定义，共有一个，但是该节点也不匹配，再去寻找 `my.*` 与 `*` 节点，均不存在，因此返回默认值 `deny`。

## 在指令器中进行权限判断

``` csharp
if (shouldAllow is false)
{
    if (await permissionManager.CheckPermission("system.command.help", cmdMessage) is false)
    {
        return CommandExecutionResult.Unauthorized;
    }
}
```


## 权限管理指令

权限的管理可以使用 `pm` 指令，帮助文档为：

``` md
- `{{PREFIX}}pm add <权限节点> <#频道名/频道 ID/*> <身份组 ID/*> <allow/deny>`  添加权限组
- `{{PREFIX}}pm set <权限 ID> [channel <#频道名/频道 ID/*>] [role <身份组 ID/*>] [value <allow/deny>]`  更新权限信息
- `{{PREFIX}}pm check <权限节点> <#频道名/频道 ID> <@用户/用户 ID>`  检查用户权限
- `{{PREFIX}}pm list [channel <#频道名/频道 ID/*>] [role <身份组 ID/*>]`  列出权限表
- `{{PREFIX}}pm remove single <权限 ID> [--dry-run]`  移除一个权限配置
- `{{PREFIX}}pm remove nodes <权限节点> [--dry-run]`  按照权限节点匹配进行移除
- `{{PREFIX}}pm remove search <#频道名/频道 ID/*> <身份组 ID/*> [--dry-run]`  按照频道与身份组检索进行移除
```

权限为：

``` txt
{ "system.command.pm.modify", "允许对权限进行新增(`add`)、修改(`set`)、删除操作(`remove`)" },
{ "system.command.pm.list", "允许使用 `pm list` 查看权限表" },
{ "system.command.pm.check", "允许使用 `pm check` 检查用户权限" }
```
