FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["PatitasAPI/PatitasAPI.csproj", "PatitasAPI/"]
RUN dotnet restore "PatitasAPI/PatitasAPI.csproj"

COPY . .
WORKDIR "/src/PatitasAPI"
RUN dotnet build "PatitasAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PatitasAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

EXPOSE 8080

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PatitasAPI.dll"]