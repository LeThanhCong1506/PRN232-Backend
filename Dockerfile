# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj files và restore dependencies trước (layer caching)
COPY ["MV.PresentationLayer/MV.PresentationLayer.csproj", "MV.PresentationLayer/"]
COPY ["MV.ApplicationLayer/MV.ApplicationLayer.csproj", "MV.ApplicationLayer/"]
COPY ["MV.InfrastructureLayer/MV.InfrastructureLayer.csproj", "MV.InfrastructureLayer/"]
COPY ["MV.DomainLayer/MV.DomainLayer.csproj", "MV.DomainLayer/"]
RUN dotnet restore "MV.PresentationLayer/MV.PresentationLayer.csproj"

# Copy toàn bộ source và build
COPY . .
WORKDIR "/src/MV.PresentationLayer"
RUN dotnet publish "MV.PresentationLayer.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080

# Copy published output
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "MV.PresentationLayer.dll"]
