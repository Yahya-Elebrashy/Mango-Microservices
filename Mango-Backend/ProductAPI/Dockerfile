FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# copy csproj (caching)
COPY ProductAPI/*.csproj ./ProductAPI/
COPY Logging/*.csproj ./Logging/

# restore
RUN dotnet restore ./ProductAPI/Mango.Services.ProductAPI.csproj

# copy full source
COPY ProductAPI/. ./ProductAPI/
COPY Logging/. ./Logging/  

# publish
RUN dotnet publish ./ProductAPI/Mango.Services.ProductAPI.csproj -c Release -o /app/publish

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "Mango.Services.ProductAPI.dll"]