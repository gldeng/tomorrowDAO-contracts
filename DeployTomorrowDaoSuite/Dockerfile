FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["DeployTomorrowDaoSuite/DeployTomorrowDaoSuite.csproj", "DeployTomorrowDaoSuite/"]
RUN dotnet restore "DeployTomorrowDaoSuite/DeployTomorrowDaoSuite.csproj"
COPY . .
WORKDIR "/src/DeployTomorrowDaoSuite"
RUN dotnet build "DeployTomorrowDaoSuite.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "DeployTomorrowDaoSuite.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeployTomorrowDaoSuite.dll"]
