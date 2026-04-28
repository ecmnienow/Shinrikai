using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;

namespace Nienow.Shinrikai.Controllers
{
    public class HomeController : Controller
    {
        public HomeController(UserManager userManager)
        {
            this._userManager = userManager;
        }

        #region Managers

        private UserManager _userManager;

        #endregion

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
