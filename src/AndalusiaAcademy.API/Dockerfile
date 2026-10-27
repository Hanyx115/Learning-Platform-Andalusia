# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER root
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys /home/app/assignment-submissions \
    && chown -R $APP_UID:$APP_UID /home/app/.aspnet /home/app/assignment-submissions
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/AndalusiaAcademy.API/AndalusiaAcademy.API.csproj", "src/AndalusiaAcademy.API/"]
COPY ["src/AndalusiaAcademy.Application/AndalusiaAcademy.Application.csproj", "src/AndalusiaAcademy.Application/"]
COPY ["src/AndalusiaAcademy.Domain/AndalusiaAcademy.Domain.csproj", "src/AndalusiaAcademy.Domain/"]
COPY ["src/AndalusiaAcademy.Infrastructure/AndalusiaAcademy.Infrastructure.csproj", "src/AndalusiaAcademy.Infrastructure/"]
RUN dotnet restore "./src/AndalusiaAcademy.API/AndalusiaAcademy.API.csproj"
COPY . .
WORKDIR "/src/src/AndalusiaAcademy.API"
RUN dotnet build "./AndalusiaAcademy.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./AndalusiaAcademy.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AndalusiaAcademy.API.dll"]
