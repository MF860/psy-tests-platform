# ========== Build stage ==========
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY PsyApi.csproj ./
RUN dotnet restore PsyApi.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ========== Runtime stage ==========
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Arabic fonts & native deps for Skia/QuestPDF
# Install Noto fonts (includes Noto Naskh Arabic), HarfBuzz, ICU for proper text shaping
RUN apt-get update && apt-get install -y --no-install-recommends \
    fonts-noto \
    fonts-noto-extra \
    fonts-noto-unhinted \
    fonts-noto-cjk \
    fonts-dejavu \
    libicu72 \
    libfontconfig1 \
    libfreetype6 \
    libharfbuzz0b \
    libharfbuzz-gobject0 \
    libharfbuzz-icu0 \
    libpng16-16 \
    libglib2.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Ensure ICU/globalization support is enabled
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
ENV PDF_FONT_FALLBACK="Noto Naskh Arabic"

# Copy published app
COPY --from=build /app/publish ./

# Render injects $PORT; bind Kestrel to it
ENV ASPNETCORE_URLS=http://0.0.0.0:${PORT:-10000}

EXPOSE 10000
ENTRYPOINT ["dotnet", "PsyApi.dll"]
