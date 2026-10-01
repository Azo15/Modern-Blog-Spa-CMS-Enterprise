# 📌 1. Aşama: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala ve restore et
COPY ["WebUI/WebUI.csproj", "WebUI/"]
COPY ["BusinessLayer/BusinessLayer.csproj", "BusinessLayer/"]
COPY ["DataAccesLayer/DataAccesLayer.csproj", "DataAccesLayer/"]
COPY ["EntityLayer/EntityLayer.csproj", "EntityLayer/"]

RUN dotnet restore "WebUI/WebUI.csproj"

# Tüm kaynak kodları kopyala ve derle
COPY . .
WORKDIR "/src/WebUI"
RUN dotnet build "WebUI.csproj" -c Release -o /app/build
RUN dotnet publish "WebUI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 📌 2. Aşama: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80

ENTRYPOINT ["dotnet", "WebUI.dll"]
