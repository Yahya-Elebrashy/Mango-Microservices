FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Mango.Services.ShoppingCartAPI/*.csproj ./Mango.Services.ShoppingCartAPI/
COPY MessageBus/*.csproj ./MessageBus/
RUN dotnet restore ./Mango.Services.ShoppingCartAPI/Mango.Services.ShoppingCartAPI.csproj

COPY Mango.Services.ShoppingCartAPI/. ./Mango.Services.ShoppingCartAPI/
COPY MessageBus/. ./MessageBus/
RUN dotnet publish ./Mango.Services.ShoppingCartAPI/Mango.Services.ShoppingCartAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.ShoppingCartAPI.dll"]