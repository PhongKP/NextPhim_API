# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy file project và restore trước để tận dụng cache của Docker
COPY ["NextPhimAPI.csproj", "./"]
RUN dotnet restore "./NextPhimAPI.csproj"

# Copy toàn bộ code và build
COPY . .
RUN dotnet publish "NextPhimAPI.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "NextPhimAPI.dll"]