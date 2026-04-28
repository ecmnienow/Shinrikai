using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection.SystemWeb;
using Microsoft.AspNetCore.Http.Abstractions;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.StaticFiles;
using Nienow.Shinrikai.Models;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.User.Model;
using Nienow.Shinrikai.Vocabulary;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;

namespace Nienow.Shinrikai.Controllers
{
    public class LoginController : Controller
    {
        public LoginController(MonitoringManager monitoringManager, UserManager userManager)
        {
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
        }

        #region Managers

        private MonitoringManager _monitoringManager;

        private UserManager _userManager;

        #endregion

        #region Private Methods

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            //Registra interação do usuário
            if (User.Identity.IsAuthenticated)
            {
                var user = this._userManager.GetUser(User.Identity.Name);
                this._monitoringManager.AddMonitor(user.ID, "Login", "Redirecionamento", returnUrl, HttpContext.Session.Id);
            }

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Public Methods

        [AllowAnonymous]
        public ActionResult Index(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public ActionResult Index(MLogin model, string returnUrl)
        {
            if (ModelState.IsValid && _userManager.Login(model.Username, model.Password))
            {
                //FormsAuthentication.SetAuthCookie(model.Username, false);


                //Registra interação do usuário
                var user = this._userManager.GetUser(model.Username);
                this._monitoringManager.AddMonitor(user.ID, "Login", "Autenticacao", "Autenticado", HttpContext.Session.Id);

                //Defina pelo menos um conjunto de claims...
                var claims = new List<Claim>
                {
                    //Atributos do usuário ...
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Role, "Administrator"),
                    new Claim("UserName", user.Name),
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true
                };

                //Loga de fato
                HttpContext.SignInAsync(
                      CookieAuthenticationDefaults.AuthenticationScheme,
                      new ClaimsPrincipal(claimsIdentity),
                      authProperties
                ).Wait();

                return RedirectToLocal(returnUrl);
            }

            model.Wrong = true;
            model.Password = string.Empty;

            return View(model);
        }

        public ActionResult Logout()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Logout", "Logout", null, HttpContext.Session.Id);

            //Request.Cookies.Remove("ASP.NET_SessionId");
            //KeyValuePair<string, string> cookie = new HttpCookie("ASP.NET_SessionId", "");
            //cookie.Expires = DateTime.Now.AddYears(-1);

            //Response.Cookies.Append("ASP.NET_SessionId", "");

            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).Wait();

            //FormsAuthentication.SignOut();
            _userManager.CurrentUser = null;

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult SignUp(TUser UserInfo)
        {
            if (UserInfo == null || UserInfo.Login == null)
                return Json(false);

            var response = this._userManager.CheckAvailability(UserInfo.Login, UserInfo.Email);

            if (!response.OK)
                return Json(response);

            response = this._userManager.SignUp(UserInfo);

            if (response.OK)
            {
                //Registra interação do usuário
                var user = this._userManager.GetUser(UserInfo.Login);
                this._monitoringManager.AddMonitor(user.ID, "SignUp", "Cadastro de usuario", UserInfo.Name, HttpContext.Session.Id);
            }

            return Json(response);
        }

        #endregion
    }
}
