FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PixelMartOrderProcessor.slnx", "./"]
COPY ["PaymentWorker/PaymentWorker.csproj", "PaymentWorker/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "PaymentWorker/PaymentWorker.csproj"

COPY . .

WORKDIR "/src/PaymentWorker"
RUN dotnet build "PaymentWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PaymentWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN groupadd --system appuser \
    && useradd --system --create-home --gid appuser --shell /bin/sh appuser

COPY --from=publish /app/publish .

RUN chown -R appuser:appuser /app

USER appuser

ENV DOTNET_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "PaymentWorker.dll"]

