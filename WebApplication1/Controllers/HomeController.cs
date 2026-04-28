using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.User.DB;
using System.Diagnostics;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(DBConnectionConfig dbConnectionConfig, UserManager userManager, ILogger<HomeController> logger)
        {
            try
            {
                var test = new DBUser(dbConnectionConfig);
                test.Connect();
            }
            catch (Exception ex)
            {
            }

            this._userManager = userManager;

            _logger = logger;
        }

        private UserManager _userManager;

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetNotification(int id, bool force)
        {
            var user = this._userManager.GetUser(User.Identity.Name);

            var message = this._userManager.GetNotification(user.ID, id, force);
            return Json(message);
        }

        [HttpPost]
        public ActionResult DeclareUnderstanding(int id)
        {
            var user = this._userManager.GetUser(User.Identity.Name);

            this._userManager.DeclareUnderstanding(user.ID, id);
            return Json(true);
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            return View();
        }
    }
}