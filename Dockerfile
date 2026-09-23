# Multi-stage build: SDK image builds and tests, runtime image serves.
#   docker build --target test .          run the suite
#   docker build -t reconcile .           build the app image
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Reconcile.slnx .
COPY src/Reconcile.Core/Reconcile.Core.csproj src/Reconcile.Core/
COPY src/Reconcile.Web/Reconcile.Web.csproj src/Reconcile.Web/
COPY tests/Reconcile.Tests/Reconcile.Tests.csproj tests/Reconcile.Tests/
RUN dotnet restore
COPY . .
RUN dotnet build --no-restore --configuration Release

FROM build AS test
RUN dotnet test --no-build --configuration Release

FROM build AS publish
RUN dotnet publish src/Reconcile.Web --no-build --configuration Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=publish /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Reconcile.Web.dll"]
