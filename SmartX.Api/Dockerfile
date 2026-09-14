FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["SmartX.Api.csproj", "./"]

RUN dotnet restore "SmartX.Api.csproj"

COPY . .

RUN dotnet build "SmartX.Api.csproj" \
    -c Release \
    -o /app/build \
    --no-restore


FROM build AS publish

RUN dotnet publish "SmartX.Api.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app

ENV ASPNETCORE_HTTP_PORTS=8080

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "SmartX.Api.dll"]