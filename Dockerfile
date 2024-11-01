FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY . .

ENV PROJECTS="dao-contract election-contract governance-contract merkletree timelock-contract treasury-contract vote-contract"

SHELL ["/bin/bash", "-c"]

RUN mkdir -p /app/contracts

# Loop over each project path
RUN for project in $PROJECTS; do \
    cd "/src/contracts/$project/src" && \
    dotnet build -c $BUILD_CONFIGURATION && \
    cp bin/$BUILD_CONFIGURATION/net8.0/*.patched /app/contracts; \
done


WORKDIR "/src/pipelines/TomorrowDAO.Cli"
RUN dotnet build -c $BUILD_CONFIGURATION -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=build /app/build .
COPY --from=build /app/contracts /app/contracts
ENTRYPOINT ["dotnet", "TomorrowDAO.Cli.dll"]
