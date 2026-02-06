FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["PixelMartOrderProcessor.slnx", "./"]
COPY ["PixelMartOrderProcessor/PixelMartOrderProcessor.csproj", "PixelMartOrderProcessor/"]
COPY ["Shared/Shared.csproj", "Shared/"]

RUN dotnet restore "PixelMartOrderProcessor/PixelMartOrderProcessor.csproj"

COPY . .

WORKDIR "/src/PixelMartOrderProcessor"
RUN dotnet build "PixelMartOrderProcessor.csproj" -c Release -o /app/build

FROM build AS publish
WORKDIR /src/PixelMartOrderProcessor
RUN dotnet publish "PixelMartOrderProcessor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

RUN groupadd --system appuser \
    && useradd --system --create-home --gid appuser --shell /bin/sh appuser

COPY --from=publish /app/publish .

RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "PixelMartOrderProcessor.dll"]