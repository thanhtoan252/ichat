FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/IChat.Core/IChat.Core.csproj src/IChat.Core/
COPY src/IChat.Infrastructure/IChat.Infrastructure.csproj src/IChat.Infrastructure/
COPY src/IChat.Api/IChat.Api.csproj src/IChat.Api/
RUN dotnet restore src/IChat.Api/IChat.Api.csproj

COPY src/ src/
RUN dotnet publish src/IChat.Api/IChat.Api.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN groupadd -r appgroup && useradd -r -g appgroup appuser \
 && mkdir -p /data/documents && chown -R appuser:appgroup /data

COPY --from=build /app/publish .
USER appuser

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "IChat.Api.dll"]
