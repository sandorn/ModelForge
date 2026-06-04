FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/backend/ModelForge.Backend/ModelForge.Backend.csproj src/backend/ModelForge.Backend/
COPY src/shared/ModelForge.Contracts/ModelForge.Contracts.csproj src/shared/ModelForge.Contracts/
RUN dotnet restore src/backend/ModelForge.Backend/ModelForge.Backend.csproj
COPY . .
RUN dotnet publish src/backend/ModelForge.Backend/ModelForge.Backend.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ModelForge.Backend.dll"]
