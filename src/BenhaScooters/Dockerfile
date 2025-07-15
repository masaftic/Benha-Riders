FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src
COPY ["src/BenhaScooters/BenhaScooters.csproj", "src/BenhaScooters/"]

RUN dotnet restore "src/BenhaScooters/BenhaScooters.csproj"

COPY . .
WORKDIR "/src/src/BenhaScooters"
RUN dotnet build "BenhaScooters.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BenhaScooters.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=publish /app/publish .

ENV ASPNETCORE_URLS=http://+:5000;
EXPOSE 5000

ENTRYPOINT ["dotnet", "BenhaScooters.dll"]
