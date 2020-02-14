using System.Linq;
using System.Web.Mvc;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Grammar.Model;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using System;
using Nienow.Shinrikai.Core;

namespace Nienow.Shinrikai.Controllers
{
    public class GrammarController : Controller
    {
        #region Managers

        private GrammarManager GrammarMng
        {
            get
            {
                if (Session["GrammarManager"] == null)
                    Session["GrammarManager"] = new GrammarManager();

                return Session["GrammarManager"] as GrammarManager;
            }
        }

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

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        [ValidateInput(false)]
        [HttpPost]
        public JsonResult AddGrammar(int id, string title, string level, string body, int[] requisites)
        {
            this.GrammarMng.AddGrammar(id, title, level, body, requisites);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult ListAllGrammars(EJLPT? jlpt)
        {
            var grammarList = this.GrammarMng.ListAllGrammars(jlpt);
            return Json(grammarList.Select(g => new { Key = g.Key, Value = g.Value }));
        }

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0)
        {
            TGrammar grammar;
            if (id == 0)
                grammar = new TGrammar();
            else
                grammar = this.GrammarMng.GetGrammar(id);

            return View(grammar);
        }

        #endregion

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de gramatica", "Pagina inicial", null, Session.SessionID);

            var grammarList = this.GrammarMng.ListGrammars(user.ID, user.JLPT);
            grammarList = grammarList.OrderBy(l => l[0].Grammar.Level).ToList();

            return View(grammarList);
        }

        [HttpPost]
        public ActionResult DeclareKnowledge(int id)
        {
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.GrammarMng.DeclareKnowledge(user.ID, id);

            return Json(true);
        }

        [HttpPost]
        public JsonResult GetGrammar(int id)
        {
            var grammar = this.GrammarMng.GetGrammar(id);

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de gramatica", "Selecao", id.ToString(), Session.SessionID);

            return Json(grammar);
        }

        [HttpPost]
        public string GetRequisites(int id)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de gramatica", "Ver requisitos", id.ToString(), Session.SessionID);

            var grammar = this.GrammarMng.GetGrammar(id);

            var hasEnoughLevel = this.GrammarMng.HasEnoughLevel(user.JLPT, grammar.Level);

            if (!hasEnoughLevel)
            {
                Response.ContentType = "html";
                return "Conteúdo ainda não disponível.";
            }

            var msg = "Conteúdo não disponível, para tal deve-se primeiramente estudar os seguintes conteúdos:";
            msg += "<ul>";

            foreach (var requisiteId in grammar.Requisites)
            {
                var requisite = this.GrammarMng.GetGrammar(requisiteId);
                msg += "<li>" + requisite.Title + "</li>";
            }

            msg += "</ul>";

            Response.ContentType = "html";
            return msg;
        }
    }
}
