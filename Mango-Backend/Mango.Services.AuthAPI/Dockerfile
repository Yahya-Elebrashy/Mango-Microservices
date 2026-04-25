FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Mango.Services.AuthAPI/*.csproj ./Mango.Services.AuthAPI/
COPY Logging/*.csproj ./Logging/

RUN dotnet restore ./Mango.Services.AuthAPI/Mango.Services.AuthAPI.csproj

COPY Mango.Services.AuthAPI/. ./Mango.Services.AuthAPI/
COPY Logging/. ./Logging/

RUN dotnet publish ./Mango.Services.AuthAPI/Mango.Services.AuthAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.AuthAPI.dll"]