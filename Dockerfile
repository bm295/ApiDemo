FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ApiDemo.Api/ApiDemo.Api.csproj ApiDemo.Api/
COPY ApiDemo.Core/ApiDemo.Core.csproj ApiDemo.Core/
RUN dotnet restore ApiDemo.Api/ApiDemo.Api.csproj

COPY . .
RUN dotnet publish ApiDemo.Api/ApiDemo.Api.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5089 5090
ENTRYPOINT ["dotnet", "ApiDemo.Api.dll"]
