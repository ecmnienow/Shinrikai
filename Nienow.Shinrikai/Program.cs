using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace WebApplication1
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            //builder.Services.AddRazorPages();
            builder.Services.AddControllersWithViews();

            //builder.Services.AddDistributedMemoryCache();

            //builder.Services.AddSession(options =>
            //{
            //    options.IdleTimeout = TimeSpan.FromSeconds(10);
            //    options.Cookie.HttpOnly = true;
            //    options.Cookie.IsEssential = true;
            //});



            //builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            //.AddCookie(opt =>
            //{
            //    opt.LoginPath = new PathString("/Conta/Login");
            //    opt.LogoutPath = new PathString("/Conta/Logout");
            //    opt.AccessDeniedPath = new PathString("/Erros/AcessoNegado");
            //    opt.Cookie = new CookieBuilder()
            //    {
            //        Name = ".NomeCookie",
            //        //AExpiration = new System.TimeSpan(0, 120, 0),
            //        //Se tiver um dominio...
            //        //Domain = ".site.com.br",
            //    };
            //});

            builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();



            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            //app.UseSession();

            //app.MapRazorPages();
            //app.MapDefaultControllerRoute();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            app.Run();
        }
    }
}