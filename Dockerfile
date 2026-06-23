# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY PMHUB.API.sln ./
COPY PMHUB.API/PMHUB.API.csproj PMHUB.API/
COPY PMHUB.Application/PMHUB.Application.csproj PMHUB.Application/
COPY PMHUB.Domain/PMHUB.Domain.csproj PMHUB.Domain/
COPY PMHUB.Infrastructure/PMHUB.Infrastructure.csproj PMHUB.Infrastructure/
COPY PMHUB.Shared/PMHUB.Shared.csproj PMHUB.Shared/

RUN dotnet restore PMHUB.API.sln

COPY . .
RUN dotnet publish PMHUB.API/PMHUB.API.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads /logs
ENTRYPOINT ["dotnet", "PMHUB.API.dll"]
