FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /ECSDiscord
COPY /ECSDiscord .
RUN dotnet restore ECSDiscord.csproj
RUN dotnet build ECSDiscord.csproj -c Release -o /app/build

FROM build AS publish
RUN dotnet publish ECSDiscord.csproj -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ECSDiscord.dll"]
