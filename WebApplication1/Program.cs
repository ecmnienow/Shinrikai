using Microsoft.Extensions.DependencyInjection;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Dictionary;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;

namespace WebApplication1
{
    public class Program
    {
        public class Settings
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();


            builder.Configuration//.SetBasePath(builder.Environment.ContentRootPath)
                                 .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            //var settings = builder.Configuration.GetSection("Settings").Get<Settings>();

            var conn = builder.Configuration.GetSection("dbconnectionconfig").Get<DBConnectionConfig>();
            //builder.Services.Configure<DBConnectionConfig>(conn);

            builder.Services.AddSingleton<DBConnectionConfig, DBConnectionConfig>(x => conn);
            //builder.Services.AddScoped<IHttpContextAccessor, HttpContextAccessor>();
            builder.Services.AddScoped<UserManager, UserManager>();
            builder.Services.AddScoped<MonitoringManager, MonitoringManager>();
            builder.Services.AddScoped<TextManager, TextManager>();
            builder.Services.AddScoped<TranscriptionManager, TranscriptionManager>();
            builder.Services.AddScoped<UserManager, UserManager>();
            builder.Services.AddScoped<KanjiManager, KanjiManager>();
            builder.Services.AddScoped<GrammarManager, GrammarManager>();
            builder.Services.AddScoped<ExercisesManager, ExercisesManager>();
            builder.Services.AddScoped<DictionaryManager, DictionaryManager>();

            var app = builder.Build();


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}