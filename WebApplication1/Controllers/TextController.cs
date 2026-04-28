using System;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.Text.Model;
using Nienow.Shinrikai.User;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Linq;
using Nienow.Shinrikai.Vocabulary;
using Microsoft.AspNetCore.Authorization;

namespace Nienow.Shinrikai.Controllers
{
    public class TextController : Controller
    {
        public TextController(MonitoringManager monitoringManager, UserManager userManager, TextManager textManager)
        {
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
            this._textManager = textManager;
        }

        #region Managers

        private MonitoringManager _monitoringManager;

        private UserManager _userManager;

        private TextManager _textManager;

        #endregion

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0)
        {
            var text = id == 0 ? new TText() : this._textManager.GetText(id);
            return View(text);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        //[ValidateInput(false)]
        [HttpPost]
        public JsonResult AddText(int id, string title, string level, string body)
        {
            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
                return Json(false);

            if (id == 0)
                this._textManager.AddText(title, level, body);
            else
                this._textManager.UpdateText(id, title, level, body);

            return Json(true);
        }

        #endregion

        #region Reading

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de textos", "Pagina inicial", null, HttpContext.Session.Id);

            var textList = this._textManager.ListTexts(user.ID, user.JLPT);
            textList = textList.OrderBy(l => l[0].Text.Level).ToList();

            return View(textList);
        }
        
        [HttpPost]
        public ActionResult DeclareKnowledge(int id)
        {
            var user = this._userManager.GetUser(User.Identity.Name);
            this._textManager.DeclareKnowledge(user.ID, id);

            return Json(true);
        }

        [HttpPost]
        public JsonResult GetText(int id)
        {
            var text = this._textManager.GetText(id);

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de textos", "Selecao", id.ToString(), HttpContext.Session.Id);

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

                if (this._textManager.UserHasWordKnowledge(user.ID, wordId) || this._textManager.UnknownStatus(user.ID, wordId))
                    newText = newText.Replace("<rt>", "<rt style=\"display: none;\">");

                text.Body = text.Body.Replace(grp.Groups[0].Value, newText);
            }

            return Json(text);
        }

        [HttpPost]
        public JsonResult RegisterLackOfKnowledge(int wordId)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de textos", "Desconhecimento de palavra", wordId.ToString(), HttpContext.Session.Id);

            this._textManager.RegisterLackOfKnowledge(user.ID, wordId);

            return Json(true);
        }

        #endregion
    }
}
