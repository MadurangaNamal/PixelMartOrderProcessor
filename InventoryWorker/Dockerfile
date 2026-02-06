FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PixelMartOrderProcessor.slnx", "./"]
COPY ["InventoryWorker/InventoryWorker.csproj", "InventoryWorker/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "InventoryWorker/InventoryWorker.csproj"

COPY . .

WORKDIR "/src/InventoryWorker"
RUN dotnet build "InventoryWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "InventoryWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN groupadd --system appuser \
    && useradd --system --create-home --gid appuser --shell /bin/sh appuser

COPY --from=publish /app/publish .

RUN chown -R appuser:appuser /app

USER appuser

ENV DOTNET_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "InventoryWorker.dll"]

