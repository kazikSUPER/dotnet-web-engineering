# 1. Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8080

# 2. SDK build image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files first for efficient caching of restored dependencies
COPY ["Services/Catalog/Catalog.Api/Catalog.Api.csproj", "Services/Catalog/Catalog.Api/"]
COPY ["Services/Catalog/Catalog.Bll/Catalog.Bll.csproj", "Services/Catalog/Catalog.Bll/"]
COPY ["Services/Catalog/Catalog.Dal/Catalog.Dal.csproj", "Services/Catalog/Catalog.Dal/"]
COPY ["Services/Catalog/Catalog.Domain/Catalog.Domain.csproj", "Services/Catalog/Catalog.Domain/"]
COPY ["Platform.Auth/Platform.Auth.csproj", "Platform.Auth/"]
COPY ["Platform.EventBus/Platform.EventBus.csproj", "Platform.EventBus/"]
COPY ["Platform.ServiceDefaults/Platform.ServiceDefaults.csproj", "Platform.ServiceDefaults/"]

# Restore packages as an independent layer
RUN dotnet restore "Services/Catalog/Catalog.Api/Catalog.Api.csproj"

# Copy remaining source code
COPY . .
WORKDIR "/src/Services/Catalog/Catalog.Api"
RUN dotnet build "Catalog.Api.csproj" -c Release -o /app/build

# 3. Publish stage
FROM build AS publish
RUN dotnet publish "Catalog.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final lightweight runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
