FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Mango.Services.OrderAPI/*.csproj ./Mango.Services.OrderAPI/
COPY MessageBus/*.csproj ./MessageBus/
RUN dotnet restore ./Mango.Services.OrderAPI/Mango.Services.OrderAPI.csproj

COPY Mango.Services.OrderAPI/. ./Mango.Services.OrderAPI/
COPY MessageBus/. ./MessageBus/
RUN dotnet publish ./Mango.Services.OrderAPI/Mango.Services.OrderAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.OrderAPI.dll"]