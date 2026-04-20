FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY ProductAPI/*.csproj ./ProductAPI/
RUN dotnet restore ./ProductAPI/Mango.Services.ProductAPI.csproj

COPY ProductAPI/. ./ProductAPI/
RUN dotnet publish ./ProductAPI/Mango.Services.ProductAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.ProductAPI.dll"]