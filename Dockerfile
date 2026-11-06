FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY . .
RUN test -f src/FoodSales.Api/wwwroot/index.html || (echo 'Package React into wwwroot before building this image.' && exit 1)
RUN dotnet publish src/FoodSales.Api/FoodSales.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0
USER root
RUN mkdir -p /home/app/keys /app/App_Data/uploads && chown -R app:app /home/app/keys /app/App_Data
USER app
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
ENV TZ=Europe/Belgrade
EXPOSE 8080
ENTRYPOINT ["dotnet", "FoodSales.Api.dll"]
