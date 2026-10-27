
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["src/EnterpriseAIAssistant.API/EnterpriseAIAssistant.API.csproj", "src/EnterpriseAIAssistant.API/"]
COPY ["src/EnterpriseAIAssistant.Application/EnterpriseAIAssistant.Application.csproj", "src/EnterpriseAIAssistant.Application/"]
COPY ["src/EnterpriseAIAssistant.Domain/EnterpriseAIAssistant.Domain.csproj", "src/EnterpriseAIAssistant.Domain/"]
COPY ["src/EnterpriseAIAssistant.Infrastructure/EnterpriseAIAssistant.Infrastructure.csproj", "src/EnterpriseAIAssistant.Infrastructure/"]

RUN dotnet restore "src/EnterpriseAIAssistant.API/EnterpriseAIAssistant.API.csproj"

COPY . .
WORKDIR /src/src/EnterpriseAIAssistant.API

RUN dotnet publish "EnterpriseAIAssistant.API.csproj" \
    -c $BUILD_CONFIGURATION \
    -o /app/publish \
    /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EnterpriseAIAssistant.API.dll"]
