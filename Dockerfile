# ─── Build aşaması ───────────────────────────────────────────────────────────
# dotnet SDK ile restore + publish tek RUN'da yapılır.
# Railpack'in iki ayrı adım (restore / publish --no-restore) kullanması
# Microsoft.EntityFrameworkCore.Analyzers paketinin kaybolmasına yol açıyordu.
# Dockerfile kullanınca Railway, railpack'i devre dışı bırakır ve bu sorunu aşarız.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish -c Release -o /app/out

# ─── Runtime aşaması ─────────────────────────────────────────────────────────
# Sadece ASP.NET runtime içerir (SDK yok) — imaj boyutu küçük kalır.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Railway $PORT değişkenini dinamik olarak atar
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-8080}

ENTRYPOINT ["dotnet", "TicketSystem.dll"]
