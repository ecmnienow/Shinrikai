// Ponto de entrada da interface. Serve o build do React (ClientApp → wwwroot) em Kestrel e encaminha /api
// para o Nienow.Shinrikai.Api (YARP, seção "ReverseProxy"), de modo que o navegador fala com uma só origem.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Os endpoints vêm da seção "Kestrel" do appsettings (no ambiente: Kestrel__Endpoints__Http__Url=...).
builder.WebHost.ConfigureKestrel(kestrel => kestrel.AddServerHeader = false);

// O endereço da API fica em ReverseProxy:Clusters:api:Destinations:shinrikai-api:Address.
builder.Services.AddReverseProxy().LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapReverseProxy();
// Rotas do React Router (ex.: /vocabulary) caem no index.html.
app.MapFallbackToFile("index.html");

app.Run();
