FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props .editorconfig ./
COPY BankingClient/BankingClient/BankingClient.csproj BankingClient/BankingClient/
RUN dotnet restore BankingClient/BankingClient/BankingClient.csproj

COPY BankingClient/BankingClient/ BankingClient/BankingClient/
RUN dotnet publish BankingClient/BankingClient/BankingClient.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "BankingClient.dll"]