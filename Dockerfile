FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["WhatsAppNotificationDashboard.csproj", "./"]
RUN dotnet restore "WhatsAppNotificationDashboard.csproj"
COPY . .
RUN dotnet build "WhatsAppNotificationDashboard.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WhatsAppNotificationDashboard.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Configurações de ambiente (podem ser sobrescritas no docker-compose ou comando docker run)
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS="http://+:8080"
ENV ConnectionStrings__DefaultConnection="Host=postgres;Port=5432;Database=bpo;Username=postgres;Password=@qowtaw%7hyzGacyvtug#;SSL Mode=Disable"

ENTRYPOINT ["dotnet", "WhatsAppNotificationDashboard.dll"]