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
using Nienow.Shinrikai.Auth;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;

namespace Nienow.Shinrikai
{
    /// <summary>
    /// Ponto de entrada da API. Substitui Global.asax + App_Start + Web.config.
    /// Hospedagem: Kestrel (sem IIS).
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            ConfigureKestrel(builder);
            var behindProxy = ConfigureReverseProxy(builder);
            ConfigureServices(builder);
            ConfigureAuthentication(builder);
            ConfigureOpenApi(builder);

            var app = builder.Build();

            if (!ShinrikaiDB.IsConfigured(app.Configuration.GetConnectionString("ShinrikaiDB")))
                app.Logger.LogWarning(
                    "ConnectionStrings:ShinrikaiDB não está configurada — toda chamada que usa o banco vai responder 503. " +
                    "Configure com: dotnet user-secrets --project Nienow.Shinrikai set \"ConnectionStrings:ShinrikaiDB\" \"...\"");

            ConfigurePipeline(app, behindProxy);
            app.Run();
        }

        /// <summary>
        /// Os endpoints vêm da seção "Kestrel" do appsettings (o host já a carrega por padrão;
        /// no ambiente use Kestrel__Endpoints__Http__Url=...). Aqui só os ajustes de servidor.
        /// </summary>
        private static void ConfigureKestrel(WebApplicationBuilder builder)
        {
            builder.WebHost.ConfigureKestrel(kestrel =>
            {
                kestrel.AddServerHeader = false;
                // Upload de imagens/áudios do cadastro de vocabulário (o endpoint ainda aplica [RequestSizeLimit]).
                kestrel.Limits.MaxRequestBodySize = 20 * 1024 * 1024;
            });
        }

        /// <summary>Atrás de um proxy reverso (nginx, Caddy, IIS como proxy) — habilitado via "ReverseProxy:Enabled".</summary>
        private static bool ConfigureReverseProxy(WebApplicationBuilder builder)
        {
            var behindProxy = builder.Configuration.GetValue<bool>("ReverseProxy:Enabled");
            if (behindProxy)
            {
                builder.Services.Configure<ForwardedHeadersOptions>(o =>
                {
                    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                    o.KnownIPNetworks.Clear();
                    o.KnownProxies.Clear();
                });
            }

            return behindProxy;
        }

        private static void ConfigureServices(WebApplicationBuilder builder)
        {
            builder.Services.Configure<ShinrikaiDbOptions>(builder.Configuration.GetSection(ShinrikaiDbOptions.SectionName));
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton<IShinrikaiDb, ShinrikaiDB>();
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddSingleton<IMonitorStore, MonitorStore>();
            builder.Services.AddSingleton<IKnowledgeStore, KnowledgeStore>();

            builder.Services.AddScoped<IMonitoringService, MonitoringService>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddScoped<IVocabularyRepository, VocabularyRepository>();
            builder.Services.AddScoped<VocabularyService>();
            builder.Services.AddScoped<ITranscriptionRepository, TranscriptionRepository>();
            builder.Services.AddScoped<TranscriptionService>();
            builder.Services.AddScoped<IKanjiRepository, KanjiRepository>();
            builder.Services.AddScoped<KanjiService>();
            builder.Services.AddScoped<IGrammarRepository, GrammarRepository>();
            builder.Services.AddScoped<GrammarService>();
            builder.Services.AddScoped<ITextRepository, TextRepository>();
            builder.Services.AddScoped<TextService>();
            builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
            builder.Services.AddScoped<ExercisesService>();
            builder.Services.AddScoped<DictionaryService>();

            builder.Services
                .AddControllers()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
            builder.Services.AddExceptionHandler<ApiExceptionHandler>();
            builder.Services.AddProblemDetails();
        }

        private static void ConfigureAuthentication(WebApplicationBuilder builder)
        {
            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer();
            builder.Services
                .AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
                .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
                {
                    bearer.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Value.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Value.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = jwt.Value.SigningKey(),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1),
                    };
                });
            builder.Services.AddAuthorization();
        }

        private static void ConfigureOpenApi(WebApplicationBuilder builder)
        {
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
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
                c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
                });
            });
        }

        private static void ConfigurePipeline(WebApplication app, bool behindProxy)
        {
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

            // Em produção, o build do React (web → wwwroot) é servido daqui.
            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.MapFallbackToFile("index.html");
        }
    }
}
