FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json ./
COPY Tanakh.Domain/Tanakh.Domain.csproj Tanakh.Domain/
COPY Tanakh.Infrastructure/Tanakh.Infrastructure.csproj Tanakh.Infrastructure/
COPY Tanakh.Api/Tanakh.Api.csproj Tanakh.Api/
RUN dotnet restore Tanakh.Api/Tanakh.Api.csproj
COPY Tanakh.Domain/ Tanakh.Domain/
COPY Tanakh.Infrastructure/ Tanakh.Infrastructure/
COPY Tanakh.Api/ Tanakh.Api/
RUN dotnet publish Tanakh.Api/Tanakh.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Tanakh.Api.dll"]
