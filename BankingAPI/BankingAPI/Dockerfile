FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props .editorconfig ./
COPY BankingAPI/BankingAPI/BankingAPI.csproj BankingAPI/BankingAPI/
RUN dotnet restore BankingAPI/BankingAPI/BankingAPI.csproj

COPY BankingAPI/BankingAPI/ BankingAPI/BankingAPI/
RUN dotnet publish BankingAPI/BankingAPI/BankingAPI.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
RUN apt-get update \
    && apt-get install --yes --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankingAPI.dll"]