FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Mango.GatewaySolution/*.csproj ./Mango.GatewaySolution/
RUN dotnet restore ./Mango.GatewaySolution/Mango.GatewaySolution.csproj

COPY Mango.GatewaySolution/. ./Mango.GatewaySolution/
RUN dotnet publish ./Mango.GatewaySolution/Mango.GatewaySolution.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.GatewaySolution.dll"]