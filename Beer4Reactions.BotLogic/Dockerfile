FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Beer4Reactions.BotLogic/Beer4Reactions.BotLogic.csproj", "Beer4Reactions.BotLogic/"]
RUN dotnet restore "Beer4Reactions.BotLogic/Beer4Reactions.BotLogic.csproj"
COPY . .
WORKDIR "/src/Beer4Reactions.BotLogic"
RUN dotnet build "./Beer4Reactions.BotLogic.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Beer4Reactions.BotLogic.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Копируем файлы настроек
COPY ["Beer4Reactions.BotLogic/appsettings.json", "./"]
COPY ["Beer4Reactions.BotLogic/appsettings.Development.json", "./"]
COPY ["Beer4Reactions.BotLogic/appsettings.Production.json", "./"]
ENTRYPOINT ["dotnet", "Beer4Reactions.BotLogic.dll"]
