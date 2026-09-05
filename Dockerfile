FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ApiDemo.csproj ./
RUN dotnet restore ApiDemo.csproj

COPY . .
RUN dotnet publish ApiDemo.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5089 5090
ENTRYPOINT ["dotnet", "ApiDemo.dll"]
