# Imagem da interface: build do React + host Kestrel que serve o SPA e encaminha /api (YARP).
# Contexto de build: a raiz do repositório.
#   docker build -f Nienow.Shinrikai.Web/Dockerfile -t shinrikai-web .
# Endereço da API no ambiente: ReverseProxy__Clusters__api__Destinations__shinrikai-api__Address.

# 1. Build do React (o vite.config.ts grava em ../wwwroot).
FROM node:22-bookworm-slim AS client
WORKDIR /src/Nienow.Shinrikai.Web/ClientApp
COPY Nienow.Shinrikai.Web/ClientApp/package.json Nienow.Shinrikai.Web/ClientApp/package-lock.json ./
RUN npm ci
COPY Nienow.Shinrikai.Web/ClientApp/ ./
RUN npm run build

# 2. Publicação do host .NET, reaproveitando o wwwroot do estágio anterior (sem rodar o npm de novo).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props ./
COPY Nienow.Shinrikai.Web/Nienow.Shinrikai.Web.csproj Nienow.Shinrikai.Web/
RUN dotnet restore Nienow.Shinrikai.Web/Nienow.Shinrikai.Web.csproj
COPY Nienow.Shinrikai.Web/ Nienow.Shinrikai.Web/
COPY --from=client /src/Nienow.Shinrikai.Web/wwwroot Nienow.Shinrikai.Web/wwwroot
RUN dotnet publish Nienow.Shinrikai.Web/Nienow.Shinrikai.Web.csproj -c Release -o /app --no-restore -p:SkipClientApp=true

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV Kestrel__Endpoints__Http__Url=http://*:8080 \
    ASPNETCORE_HTTP_PORTS=
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Nienow.Shinrikai.Web.dll"]
