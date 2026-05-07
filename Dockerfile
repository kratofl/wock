FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Wock.sln ./
COPY src/Wock/Wock.csproj src/Wock/
COPY src/Wock.Abstractions/Wock.Abstractions.csproj src/Wock.Abstractions/
COPY tests/Wock.Tests/Wock.Tests.csproj tests/Wock.Tests/
RUN dotnet restore Wock.sln

COPY . .
RUN dotnet publish src/Wock/Wock.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ConnectionStrings__WockDb="Data Source=/data/wock.db"
ENV Plugins__StoragePath=/plugins

RUN mkdir /data /plugins
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Wock.dll"]
