FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

COPY Mango.Services.CouponAPI/*.csproj ./Mango.Services.CouponAPI/
RUN dotnet restore ./Mango.Services.CouponAPI/Mango.Services.CouponAPI.csproj

COPY Mango.Services.CouponAPI/. ./Mango.Services.CouponAPI/
RUN dotnet publish ./Mango.Services.CouponAPI/Mango.Services.CouponAPI.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Mango.Services.CouponAPI.dll"]