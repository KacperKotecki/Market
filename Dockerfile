# Etap 1: Budowanie (SDK Image)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Kopiujemy plik solucji i projektu aby pobrać biblioteki (warstwowanie cache)
COPY ["Market.sln", "./"]
COPY ["Market.Web/Market.Web.csproj", "Market.Web/"]

# Restore zależności
RUN dotnet restore "Market.sln"

# Kopiujemy resztę kodu
COPY . .

# Build i Publish
WORKDIR "/src/Market.Web"
RUN dotnet publish "Market.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etap 2: Runtime (Lżejszy obraz)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Kopiujemy zbudowane pliki z etapu 1
COPY --from=build /app/publish .

# Tworzymy folder na uploady (ważne dla trwałości zdjęć)
RUN mkdir -p /app/wwwroot/uploads

ENTRYPOINT ["dotnet", "Market.Web.dll"]