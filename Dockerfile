FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["src/StudShare.Api/StudShare.Api.csproj", "src/StudShare.Api/"]
COPY ["src/StudShare.Application/StudShare.Application.csproj", "src/StudShare.Application/"]
COPY ["src/StudShare.Domain/StudShare.Domain.csproj", "src/StudShare.Domain/"]
COPY ["src/StudShare.Infrastructure/StudShare.Infrastructure.csproj", "src/StudShare.Infrastructure/"]
RUN dotnet restore "src/StudShare.Api/StudShare.Api.csproj"

COPY . .
RUN dotnet publish "src/StudShare.Api/StudShare.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StudShare.Api.dll"]
