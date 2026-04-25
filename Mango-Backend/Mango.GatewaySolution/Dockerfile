FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj files 
COPY Mango.GatewaySolution/*.csproj ./Mango.GatewaySolution/
COPY Logging/*.csproj ./Logging/

# Restore dependencies
RUN dotnet restore ./Mango.GatewaySolution/Mango.GatewaySolution.csproj

# Copy full source code
COPY Mango.GatewaySolution/. ./Mango.GatewaySolution/
COPY Logging/. ./Logging/

# Publish
RUN dotnet publish ./Mango.GatewaySolution/Mango.GatewaySolution.csproj -c Release -o /app/publish

# Runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Mango.GatewaySolution.dll"]