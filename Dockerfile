FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

WORKDIR /src
COPY ./src .

RUN dotnet restore
RUN dotnet publish -c Release -o /app

FROM base AS runtime

WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "importer.dll"]
