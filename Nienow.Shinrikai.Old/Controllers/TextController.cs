using System;
using System.Web.Mvc;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.Text.Model;
using Nienow.Shinrikai.User;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;

namespace Nienow.Shinrikai.Controllers
{
    public class TextController : Controller
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

        private TextManager TextMng
        {
            get
            {
                if (Session["TextManager"] == null)
                    Session["TextManager"] = new TextManager();

                return Session["TextManager"] as TextManager;
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

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0)
        {
            var text = id == 0 ? new TText() : this.TextMng.GetText(id);
            return View(text);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [ValidateInput(false)]
        [HttpPost]
        public JsonResult AddText(int id, string title, string level, string body)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
                return Json(false);

            if (id == 0)
                this.TextMng.AddText(title, level, body);
            else
                this.TextMng.UpdateText(id, title, level, body);

            return Json(true);
        }

        #endregion

        #region Reading

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de textos", "Pagina inicial", null, Session.SessionID);

            var textList = this.TextMng.ListTexts(user.ID, user.JLPT);
            textList = textList.OrderBy(l => l[0].Text.Level).ToList();

            return View(textList);
        }
        
        [HttpPost]
        public ActionResult DeclareKnowledge(int id)
        {
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.TextMng.DeclareKnowledge(user.ID, id);

            return Json(true);
        }

        [HttpPost]
        public JsonResult GetText(int id)
        {
            var text = this.TextMng.GetText(id);

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de textos", "Selecao", id.ToString(), Session.SessionID);

            var regex = new Regex("<span class=\"stretchText\" data-id=\"([0-9]{0,})\">.*?<rt\\s{0,}>.*?<\\/rt>.*?<\\/span>");

            foreach (Match grp in regex.Matches(text.Body))
            {
                var newText = grp.Groups[0].Value;

                if (string.IsNullOrEmpty(grp.Groups[1].Value))
                {
                    Trace.TraceWarning(@"Streatch text missing word referenced ID. => {0}", newText);
                    continue;
                }

                var wordId = Convert.ToInt32(grp.Groups[1].Value);

                if (this.TextMng.UserHasWordKnowledge(user.ID, wordId) || this.TextMng.UnknownStatus(user.ID, wordId))
                    newText = newText.Replace("<rt>", "<rt style=\"display: none;\">");

                text.Body = text.Body.Replace(grp.Groups[0].Value, newText);
            }

            return Json(text);
        }

        [HttpPost]
        public JsonResult RegisterLackOfKnowledge(int wordId)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de textos", "Desconhecimento de palavra", wordId.ToString(), HttpContext.Session.SessionID);

            this.TextMng.RegisterLackOfKnowledge(user.ID, wordId);

            return Json(true);
        }

        #endregion
    }
}
