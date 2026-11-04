// Ponto de entrada da API. Substitui Global.asax + App_Start + Web.config. Hospedagem: Kestrel (sem IIS).
// Só atende /api (e /swagger em dev); a interface é servida pelo projeto Nienow.Shinrikai.Web.
using System;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Api;
using Nienow.Shinrikai.Api.Auth;
using Nienow.Shinrikai.Api.Controllers;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Os endpoints vêm da seção "Kestrel" do appsettings (no ambiente: Kestrel__Endpoints__Http__Url=...).
builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    // Upload de imagens/áudios dos cadastros (os endpoints ainda aplicam [RequestSizeLimit]).
    kestrel.Limits.MaxRequestBodySize = ShinrikaiController.MaxUploadBytes;
});

// Atrás de um proxy reverso (nginx, Caddy, IIS como proxy) — habilitado via "ReverseProxy:Enabled".
var behindProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
if (behindProxy)
    services.Configure<ForwardedHeadersOptions>(o =>
    {
        o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        o.KnownIPNetworks.Clear();
        o.KnownProxies.Clear();
    });

services.Configure<ShinrikaiDbOptions>(builder.Configuration.GetSection(ShinrikaiDbOptions.SectionName));
services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

services.AddSingleton(TimeProvider.System);
services.AddSingleton<IShinrikaiDb, ShinrikaiDB>();
services.AddSingleton<JwtTokenService>();
services.AddSingleton<IMonitorStore, MonitorStore>();
services.AddSingleton<IKnowledgeStore, KnowledgeStore>();

services.AddScoped<IMonitoringService, MonitoringService>();
services.AddScoped<IUserRepository, UserRepository>().AddScoped<UserService>();
services.AddScoped<IVocabularyRepository, VocabularyRepository>().AddScoped<VocabularyService>();
services.AddScoped<ITranscriptionRepository, TranscriptionRepository>().AddScoped<TranscriptionService>();
services.AddScoped<IKanjiRepository, KanjiRepository>().AddScoped<KanjiService>();
services.AddScoped<IGrammarRepository, GrammarRepository>().AddScoped<GrammarService>();
services.AddScoped<ITextRepository, TextRepository>().AddScoped<TextService>();
services.AddScoped<IExerciseRepository, ExerciseRepository>().AddScoped<ExercisesService>();
services.AddScoped<DictionaryService>();

services.AddControllers().AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
services.AddExceptionHandler<ApiExceptionHandler>();
services.AddProblemDetails();

services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((bearer, jwt) => bearer.TokenValidationParameters = new TokenValidationParameters
    {
        // ValidateIssuer, ValidateAudience e ValidateLifetime já são true por padrão; a chave não.
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwt.Value.Issuer,
        ValidAudience = jwt.Value.Audience,
        IssuerSigningKey = jwt.Value.SigningKey(),
        ClockSkew = TimeSpan.FromMinutes(1),
    });
services.AddAuthorization();

services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Shinrikai API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
    });
    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", document)] = [] });
});

var app = builder.Build();

if (!ShinrikaiDB.IsConfigured(app.Configuration.GetConnectionString("ShinrikaiDB")))
    app.Logger.LogWarning("Toda chamada que usa o banco vai responder 503. {Message}", ShinrikaiDB.NotConfiguredMessage);

if (behindProxy)
    app.UseForwardedHeaders();

app.UseExceptionHandler();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
