using Nienow.Shinrikai.Models;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.User.Model;
using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Nienow.Shinrikai.Controllers
{
    public class LoginController : Controller
    {
        #region Managers

        private MonitoringManager MonitoringMng
        {
            get
            {
                if (Session["MonitoringManager"] == null)
                    Session["MonitoringManager"] = new MonitoringManager();

                return Session["MonitoringManager"] as MonitoringManager;
            }
        }

        private UserManager UserMng
        {
            get
            {
                if (Session["UserManager"] == null)
                    Session["UserManager"] = new UserManager();

                return Session["UserManager"] as UserManager;
            }
        }

        #endregion

        #region Private Methods

        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            //Registra interação do usuário
            if (User.Identity.IsAuthenticated)
            {
                var user = this.UserMng.GetUser(User.Identity.Name);
                this.MonitoringMng.AddMonitor(user.ID, "Login", "Redirecionamento", returnUrl, Session.SessionID);
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
            if (ModelState.IsValid && UserMng.Login(model.Username, model.Password))
            {
                FormsAuthentication.SetAuthCookie(model.Username, false);

                //Registra interação do usuário
                var user = this.UserMng.GetUser(model.Username);
                this.MonitoringMng.AddMonitor(user.ID, "Login", "Autenticacao", "Autenticado", Session.SessionID);

                return RedirectToLocal(returnUrl);
            }

            model.Wrong = true;
            model.Password = string.Empty;

            return View(model);
        }

        public ActionResult Logout()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Logout", "Logout", null, Session.SessionID);
            
            Request.Cookies.Remove("ASP.NET_SessionId");
            HttpCookie cookie = new HttpCookie("ASP.NET_SessionId", "");
            cookie.Expires = DateTime.Now.AddYears(-1);
            Response.Cookies.Add(cookie);

            FormsAuthentication.SignOut();
            UserMng.CurrentUser = null;

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        public JsonResult SignUp(TUser UserInfo)
        {
            if (UserInfo == null || UserInfo.Login == null)
                return Json(false);

            var response = this.UserMng.CheckAvailability(UserInfo.Login, UserInfo.Email);

            if (!response.OK)
                return Json(response);

            response = this.UserMng.SignUp(UserInfo);

            if (response.OK)
            {
                //Registra interação do usuário
                var user = this.UserMng.GetUser(UserInfo.Login);
                this.MonitoringMng.AddMonitor(user.ID, "SignUp", "Cadastro de usuario", UserInfo.Name, Session.SessionID);
            }

            return Json(response);
        }

        #endregion
    }
}
