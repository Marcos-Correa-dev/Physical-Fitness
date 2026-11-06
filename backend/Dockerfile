# ====================================================================
# Dockerfile Multi-Stage para KmValidator.Api (.NET 8)
# Otimizado para AWS ECS Fargate Spot (mínimo consumo de CPU e RAM)
# ====================================================================

# 1. Estágio de Compilação
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Cache de dependências do NuGet
COPY ["src/KmValidator.Domain/KmValidator.Domain.csproj", "KmValidator.Domain/"]
COPY ["src/KmValidator.Application/KmValidator.Application.csproj", "KmValidator.Application/"]
COPY ["src/KmValidator.Infrastructure/KmValidator.Infrastructure.csproj", "KmValidator.Infrastructure/"]
COPY ["src/KmValidator.Api/KmValidator.Api.csproj", "KmValidator.Api/"]

RUN dotnet restore "KmValidator.Api/KmValidator.Api.csproj"

# Copia código-fonte e compila
COPY src/ .
WORKDIR "/src/KmValidator.Api"
RUN dotnet publish "KmValidator.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 2. Estágio Final de Runtime (Alpine Linux ~110MB)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Execução com usuário sem privilégios root para segurança
USER app

EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "KmValidator.Api.dll"]
