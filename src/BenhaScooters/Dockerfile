FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy centralized package props and project files for layer caching
COPY ["Directory.Packages.props", "./"]
COPY ["src/BenhaScooters.Contracts/BenhaScooters.Contracts.csproj", "src/BenhaScooters.Contracts/"]
COPY ["src/BenhaScooters.Shared/BenhaScooters.Shared.csproj", "src/BenhaScooters.Shared/"]
COPY ["src/BenhaScooters/BenhaScooters.csproj", "src/BenhaScooters/"]

RUN dotnet restore "src/BenhaScooters/BenhaScooters.csproj"

# Copy source files and publish
COPY ["src/", "src/"]
WORKDIR "/src/src/BenhaScooters"
RUN dotnet publish "BenhaScooters.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

ENTRYPOINT ["dotnet", "BenhaScooters.dll"]
