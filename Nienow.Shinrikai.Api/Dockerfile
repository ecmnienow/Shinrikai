# syntax=docker/dockerfile:1
# Imagem da API (só /api). Contexto de build: a raiz do repositório.
#   docker build -f Nienow.Shinrikai.Api/Dockerfile -t shinrikai-api .
# Configuração no ambiente: ConnectionStrings__ShinrikaiDB e Jwt__Key (obrigatórios).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primeiro só os .csproj: o restore fica em cache enquanto as dependências não mudam.
# --parents mantém as pastas, então um módulo novo entra sem mexer aqui.
COPY global.json Directory.Build.props ./
COPY --parents */*.csproj ./
RUN dotnet restore Nienow.Shinrikai.Api/Nienow.Shinrikai.Api.csproj

COPY . .
RUN dotnet publish Nienow.Shinrikai.Api/Nienow.Shinrikai.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app .

# O appsettings.json escuta em localhost:5080; no container precisa ser em todas as interfaces.
# ASPNETCORE_HTTP_PORTS (8080 na imagem base) é limpo para o Kestrel não avisar que o sobrescreve.
ENV Kestrel__Endpoints__Http__Url=http://*:8080 \
    ASPNETCORE_HTTP_PORTS=
EXPOSE 8080
# Usuário sem privilégios que a imagem do .NET já traz.
USER $APP_UID
ENTRYPOINT ["dotnet", "Nienow.Shinrikai.Api.dll"]
