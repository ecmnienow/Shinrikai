using System.Web.Mvc;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Controllers
{
    public class HomeController : Controller
    {
        #region Managers

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

        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public JsonResult GetNotification(int id, bool force)
        {
            var user = this.UserMng.GetUser(User.Identity.Name);

            var message = this.UserMng.GetNotification(user.ID, id, force);
            return Json(message);
        }
        
        [HttpPost]
        public ActionResult DeclareUnderstanding(int id)
        {
            var user = this.UserMng.GetUser(User.Identity.Name);

            this.UserMng.DeclareUnderstanding(user.ID, id);
            return Json(true);
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            return View();
        }
    }
}
