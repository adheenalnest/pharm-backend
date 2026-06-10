FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
# Trust corporate Sophos SSL inspection CA
COPY ["sophos-root-ca.crt", "/usr/local/share/ca-certificates/sophos-root-ca.crt"]
COPY ["sophos-ssl-ca.crt", "/usr/local/share/ca-certificates/sophos-ssl-ca.crt"]
RUN update-ca-certificates
WORKDIR /src
# Copy pre-downloaded packages so restore works offline (corporate proxy blocks NuGet downloads)
COPY ["packages/", "/nuget-packages/"]
COPY ["PharmeasyAPI.csproj", "."]
ENV NUGET_PACKAGES=/nuget-packages
RUN dotnet restore "./PharmeasyAPI.csproj"
COPY . .
RUN dotnet build "PharmeasyAPI.csproj" -c Release -o /app/build --no-restore

FROM build AS publish
RUN dotnet publish "PharmeasyAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PharmeasyAPI.dll"]
