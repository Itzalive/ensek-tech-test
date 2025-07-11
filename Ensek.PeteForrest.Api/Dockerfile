# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS restore
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Ensek.PeteForrest.Api/Ensek.PeteForrest.Api.csproj", "Ensek.PeteForrest.Api/"]
COPY ["Ensek.PeteForrest.Infrastructure/Ensek.PeteForrest.Infrastructure.csproj", "Ensek.PeteForrest.Infrastructure/"]
COPY ["Ensek.PeteForrest.Domain/Ensek.PeteForrest.Domain.csproj", "Ensek.PeteForrest.Domain/"]
COPY ["Ensek.PeteForrest.Services/Ensek.PeteForrest.Services.csproj", "Ensek.PeteForrest.Services/"]
RUN dotnet restore "./Ensek.PeteForrest.Api/Ensek.PeteForrest.Api.csproj"

FROM restore as build
COPY . .
WORKDIR "/src/Ensek.PeteForrest.Api"
RUN dotnet build "./Ensek.PeteForrest.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Ensek.PeteForrest.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Ensek.PeteForrest.Api.dll"]