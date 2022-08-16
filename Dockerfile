FROM mcr.microsoft.com/dotnet/nightly/runtime:7.0

ARG APP_VERSION=0.0.0

WORKDIR /app

COPY ./publish /app/

RUN ["mkdir", "/app/data"]
RUN ["mkdir", "/app/data/pluginCache"]
RUN ["mkdir", "/app/data/plugins"]
RUN ["mkdir", "/app/data/logs"]

# CORE
ENV DODO_HOSTED_PLUGIN_CACHE_DIRECTORY="/app/data/pluginCache"
ENV DODO_HOSTED_PLUGIN_DIRECTORY="/app/data/plugins"

ENV DODO_HOSTED_MONGO_CONNECTION_STRING="mongodb://mongo:27017"
ENV DODO_HOSTED_MONGO_DATABASE_NAME="dodo-hosted"

ENV DODO_HOSTED_ADMIN_ISLAND=""
ENV DODO_HOSTED_COMMAND_PREFIX="!"
ENV DODO_HOSTED_OPENAPI_LOG_LEVEL="Debug"

# SDK
ENV DODO_SDK_BOT_CLIENT_ID=""
ENV DODO_SDK_BOT_TOKEN=""
ENV DODO_SDK_API_ENDPOINT="https://botopen.imdodo.com"

# APP
ENV DODO_HOSTED_APP_LOGGER_MINIMUM_LEVEL="Information"
ENV DODO_HOSTED_APP_LOGGER_SINK_TO_FILE=""
ENV DODO_HOSTED_APP_LOGGER_SINK_TO_FILE_ROLLING_INTERVAL="Day"

# DO NOT CHANGE
ENV DODO_HOSTED_RUNTIME_CONTAINER=true
ENV DODO_HOSTED_VERSION=$APP_VERSION

VOLUME ["/app/data/plugins"]
VOLUME ["/app/data/logs"]

ENTRYPOINT ["dotnet", "DodoHosted.App.dll"]
