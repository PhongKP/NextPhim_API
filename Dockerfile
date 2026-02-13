# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["NextPhimAPI/NextPhimAPI.csproj", "NextPhimAPI/"]
RUN dotnet restore "NextPhimAPI/NextPhimAPI.csproj"

COPY . .

WORKDIR "/src/NextPhimAPI"
RUN dotnet publish "NextPhimAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NextPhimAPI.dll"]