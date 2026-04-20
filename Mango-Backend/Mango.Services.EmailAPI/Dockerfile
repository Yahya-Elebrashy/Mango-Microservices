FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Mango.Services.EmailAPI/*.csproj ./Mango.Services.EmailAPI/
COPY MessageBus/*.csproj ./MessageBus/
RUN dotnet restore ./Mango.Services.EmailAPI/Mango.Services.EmailAPI.csproj

COPY Mango.Services.EmailAPI/. ./Mango.Services.EmailAPI/
COPY MessageBus/. ./MessageBus/
RUN dotnet publish ./Mango.Services.EmailAPI/Mango.Services.EmailAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.EmailAPI.dll"]