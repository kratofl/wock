FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Wock.sln ./
COPY src/Wock/Wock.csproj src/Wock/
COPY src/Wock.Abstractions/Wock.Abstractions.csproj src/Wock.Abstractions/
COPY src/Wock.Domain/Wock.Domain.csproj src/Wock.Domain/
COPY src/Wock.Application/Wock.Application.csproj src/Wock.Application/
COPY src/Wock.Infrastructure/Wock.Infrastructure.csproj src/Wock.Infrastructure/
COPY src/Wock.Migrations.Sqlite/Wock.Migrations.Sqlite.csproj src/Wock.Migrations.Sqlite/
COPY src/Wock.Migrations.SqlServer/Wock.Migrations.SqlServer.csproj src/Wock.Migrations.SqlServer/
COPY src/Wock.Migrations.Postgres/Wock.Migrations.Postgres.csproj src/Wock.Migrations.Postgres/
COPY tests/Wock.Tests/Wock.Tests.csproj tests/Wock.Tests/
RUN dotnet restore Wock.sln

COPY . .
RUN dotnet publish src/Wock/Wock.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV Database__Provider=Sqlite
ENV Database__ConnectionString="Data Source=/data/wock.db"
ENV Plugins__StoragePath=/plugins

RUN mkdir /data /plugins
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Wock.dll"]
