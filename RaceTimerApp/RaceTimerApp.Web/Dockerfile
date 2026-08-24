FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["RaceTimerApp/RaceTimerApp.Web/RaceTimerApp.Web.csproj", "RaceTimerApp/RaceTimerApp.Web/"]
COPY ["RaceTimerApp/RaceTimerApp.Web.Client/RaceTimerApp.Web.Client.csproj", "RaceTimerApp/RaceTimerApp.Web.Client/"]
COPY ["RaceTimerApp/RaceTimerApp.Shared/RaceTimerApp.Shared.csproj", "RaceTimerApp/RaceTimerApp.Shared/"]
COPY ["RaceTimerShared/RaceTimerShared.csproj", "RaceTimerShared/"]
RUN dotnet restore "RaceTimerApp/RaceTimerApp.Web/RaceTimerApp.Web.csproj"
COPY . .
WORKDIR "/src/RaceTimerApp/RaceTimerApp.Web"
RUN dotnet publish "RaceTimerApp.Web.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceTimerApp.Web.dll"]
