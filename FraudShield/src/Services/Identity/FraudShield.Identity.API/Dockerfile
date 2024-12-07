FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Services/Identity/FraudShield.Identity.API/FraudShield.Identity.API.csproj", "src/Services/Identity/FraudShield.Identity.API/"]
COPY ["src/Services/Identity/FraudShield.Identity.Application/FraudShield.Identity.Application.csproj", "src/Services/Identity/FraudShield.Identity.Application/"]
COPY ["src/Services/Identity/FraudShield.Identity.Domain/FraudShield.Identity.Domain.csproj", "src/Services/Identity/FraudShield.Identity.Domain/"]
COPY ["src/Services/Identity/FraudShield.Identity.Infrastructure/FraudShield.Identity.Infrastructure.csproj", "src/Services/Identity/FraudShield.Identity.Infrastructure/"]
RUN dotnet restore "src/Services/Identity/FraudShield.Identity.API/FraudShield.Identity.API.csproj"
COPY . .
WORKDIR "/src/src/Services/Identity/FraudShield.Identity.API"
RUN dotnet build "FraudShield.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "FraudShield.Identity.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FraudShield.Identity.API.dll"]
