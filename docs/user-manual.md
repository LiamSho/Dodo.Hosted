# 用户手册

DodoHosted 是使用 `.NET 7.0` 编写的 Dodo 渡渡语音机器人框架。

DodoHosted 是一整个程序集合，在用户手册中，提到 DodoHosted 均指的是 DodoHosted 机器人框架宿主程序。

## 使用 Docker 运行

DodoHosted 提供了 Docker 镜像，推荐使用 Docker 进行部署。

接下去的内容默认你需要 Docker 与 Docker Compose 有基本的了解，并且在一台 Linux 服务器上安装了 Docker Engine 与 Docker Compose，或者在 Windows，macOS，Linux 桌面系统安装了 Docker Desktop。

这里给出几个 Docker 参考教程：

- [[官网] Docker 官方文档](https://docs.docker.com/get-started/)
- [[YouTube] Docker Tutorial for Beginners (by TechWorld with Nana)](https://www.youtube.com/watch?v=3c-iBn73dDE) (推荐)
- [[Bilibili] Docker 1小时快速上手教程，无废话纯干货 (by 广州云科)](https://www.bilibili.com/video/BV11L411g7U1/)

### 从 Docker Hub 获取镜像
    
DodoHosted 的镜像支持 linux/arm64 以及 linux/amd64 架构。
    
```shell
  docker pull alisaqaq/dodo-hosted:latest
```
    
TAG 可以为具体的版本，例如，要使用 3.0.0 版本，则可以使用：
    
```shell
docker pull alisaqaq/dodo-hosted:3.0.0
```

DodoHosted 需要使用 MongoDB 作为数据库，如果需要同时部署一个 MongoDB，则需要同时获取 MongoDB 的镜像：

```shell
docker pull mongo:5
```

### 创建 Docker Compose 文件

创建一个空的目录，在该目录内创建一个 `docker-compose.yaml` 文件，模版如下：

```yaml
version: "3"

services:
  
  dodo-hosted:
    image: alisaqaq/dodo-hosted:latest
    container_name: dodo-hosted
    restart: always
    volumes:
      - ./plugins:/app/data/plugins
      - ./logs:/app/data/logs
    environment:
      # 数据库配置
      - DODO_HOSTED_MONGO_CONNECTION_STRING=mongodb://mongo:27017
      - DODO_HOSTED_MONGO_DATABASE_NAME="dodo-hosted"
      
      # 管理配置
      - DODO_HOSTED_ADMIN_ISLAND="123456"
      - DODO_HOSTED_COMMAND_PREFIX="!"
      - DODO_HOSTED_OPENAPI_LOG_LEVEL="Debug"
        
      # 机器人配置
      - DODO_SDK_BOT_CLIENT_ID="YOUR BOT CLIENT ID HERE"
      - DODO_SDK_BOT_TOKEN="BOT TOKEN HERE"
      - DODO_SDK_API_ENDPOINT="https://botopen.imdodo.com"
        
      # Host 配置
      - DODO_HOSTED_APP_LOGGER_MINIMUM_LEVEL="Information"
      - DODO_HOSTED_APP_LOGGER_SINK_TO_FILE="/app/data/logs/log-.log"
      - DODO_HOSTED_APP_LOGGER_SINK_TO_FILE_ROLLING_INTERVAL="Day"
      - DODO_HOSTED_WEB_MASTER_TOKEN="YOUR MASTER TOKEN HERE"
      - DODO_HOSTED_WEB_BEHIND_PROXY=true
    ports:
      - "80:80"
  
  mongodb:
    image: mongo:5
    container_name: mongodb
    restart: always
    volumes:
      - mongodb-data:/data/db
    ports:
      - "27017:27017"

volumes:
  mongodb-data:
```

如果不需要 MongoDB，可以移除 `mongodb` 段的内容。

#### 环境变量 Environment

DodoHosted 的 Docker 镜像主要通过环境变量进行配置。

| 环境变量                                                 | 默认值                        | 说明                                                                                                             |
|------------------------------------------------------|----------------------------|----------------------------------------------------------------------------------------------------------------|
| DODO_HOSTED_MONGO_CONNECTION_STRING                  | mongodb://mongo:27017      | MongoDB 数据库连接字符串                                                                                               |
| DODO_HOSTED_MONGO_DATABASE_NAME                      | dodo-hosted                | MongoDB 数据库名称                                                                                                  |
| DODO_HOSTED_ADMIN_ISLAND                             |                            | 管理群组 ID                                                                                                        |
| DODO_HOSTED_COMMAND_PREFIX                           | !                          | 指令前缀，必须为一个字符                                                                                                   |
| DODO_HOSTED_OPENAPI_LOG_LEVEL                        | Debug                      | Dodo OpenAPI 日志等级，可选值为 `Trace` `Debug` `Information` `Warning` `Error` `Critical`，其他任何值都默认为 `Debug`            |
| DODO_SDK_BOT_CLIENT_ID                               |                            | 机器人 CLIENT ID，从 Dodo 开放平台获取                                                                                    |
| DODO_SDK_BOT_TOKEN                                   |                            | 机器人 TOKEN，从开放平台获取                                                                                              |
| DODO_SDK_API_ENDPOINT                                | https://botopen.imdodo.com | 机器人 API 终结点，从开放平台获取，一般不需要设置此项                                                                                  |
| DODO_HOSTED_APP_LOGGER_MINIMUM_LEVEL                 | Information                | 宿主程序日志记录等级，默认为 `Information`，可选值为 `Trace` `Debug` `Information` `Warning` `Error` `Critical`，其他任何值都默认为 `Debug` |
| DODO_HOSTED_APP_LOGGER_SINK_TO_FILE                  |                            | 宿主程序日志记录到文件，默认为空，即不开启，若开启，请将此项设置为 `/app/data/logs/log-.log`                                                    |
| DODO_HOSTED_APP_LOGGER_SINK_TO_FILE_ROLLING_INTERVAL | Day                        | 宿主程序日志文件记录滚动周期，只在开启记录日志到文件时起作用，可选值为 `Infinite` `Year` `Month` `Day` `Hour` `Minute`，其他任何值都为 `Day`              |
| DODO_HOSTED_WEB_MASTER_TOKEN                         |                            | DodoHosted Web API Master Token                                                                                |
| DODO_HOSTED_WEB_BEHIND_PROXY                         | false                      | DodoHosted Web API 是否在反向代理之后                                                                                   |

#### 卷 Volume

DodoHosted 的 Docker 镜像向外暴露两个 Volume，分别为 `/app/data/plugins` 和 `/app/data/logs`。

- `/app/data/plugins` 存放插件文件包
- `/app/data/logs` 存放日志文件，如果不需要记录日志到文件，可以不挂载此 Volume

#### 端口 Port

DodoHosted 的 WebAPI 监听容器内的 80 端口。

### 运行

在 `docker-compose.yaml` 文件所在目录使用 Docker Compose 启动容器：

```shell
docker-compose up -d
```

## 管理

### 日志

如果开启了日志记录到文件，可以直接查看挂载到宿主机的日志文件目录中的文件。

或者，可以使用 `docker logs` 命令查看容器的日志：

```shell
docker logs dodo-hosted
```

### 插件

只需要将插件文件包存放到挂载到 `/app/data/plugins` 的目录中即可。

所有的插件均会在容器启动时自动加载，在启动后新增的插件包可以在管理群组中使用 `system plugin load -p <包名>` 指令载入。

插件管理相关的指令可以在管理群组中使用 `system plugin -?` 查看。

### 更新

如果需要更新 DodoHosted，可以使用 `docker pull` 命令拉取最新的镜像：

```shell
docker pull imdodo/dodo-hosted:latest
```

然后使用 `docker-compose up -d` 命令重启容器即可。

插件更新则只需要将新的插件文件包放到挂载到 `/app/data/plugins` 的目录中，然后在管理群组中使用指令重载即可。

需要注意的是，更新时，插件兼容的 API 版本是否兼容 DodoHosted 的 API 版本，如果不兼容，可能会导致插件无法正常工作。
