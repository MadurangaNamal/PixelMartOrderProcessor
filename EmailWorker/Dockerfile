FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PixelMartOrderProcessor.slnx", "./"]
COPY ["EmailWorker/EmailWorker.csproj", "EmailWorker/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "EmailWorker/EmailWorker.csproj"

COPY . .

WORKDIR "/src/EmailWorker"
RUN dotnet build "EmailWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmailWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Create a non-root user for security
RUN groupadd --system appuser \
    && useradd --system --create-home --gid appuser --shell /bin/sh appuser

COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R appuser:appuser /app

# Switch to non-root user
USER appuser

ENV DOTNET_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "EmailWorker.dll"]


