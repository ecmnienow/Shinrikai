using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Grammar.Model;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using System;
using Nienow.Shinrikai.Core;
using Microsoft.AspNetCore.Authorization;
using Nienow.Shinrikai.Vocabulary;

namespace Nienow.Shinrikai.Controllers
{
    public class GrammarController : Controller
    {
        public GrammarController(MonitoringManager monitoringManager, UserManager userManager, GrammarManager grammarManager)
        {
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
            this._grammarManager = grammarManager;
        }

        #region Managers

        private MonitoringManager _monitoringManager;

        private UserManager _userManager;

        private GrammarManager _grammarManager;

        #endregion

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        //[ValidateInput(false)]
        [HttpPost]
        public JsonResult AddGrammar(int id, string title, string level, string body, int[] requisites)
        {
            this._grammarManager.AddGrammar(id, title, level, body, requisites);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult ListAllGrammars(EJLPT? jlpt)
        {
            var grammarList = this._grammarManager.ListAllGrammars(jlpt);
            return Json(grammarList.Select(g => new { Key = g.Key, Value = g.Value }));
        }

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0)
        {
            TGrammar grammar;
            if (id == 0)
                grammar = new TGrammar();
            else
                grammar = this._grammarManager.GetGrammar(id);

            return View(grammar);
        }

        #endregion

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de gramatica", "Pagina inicial", null, HttpContext.Session.Id);

            var grammarList = this._grammarManager.ListGrammars(user.ID, user.JLPT);
            grammarList = grammarList.OrderBy(l => l[0].Grammar.Level).ToList();

            return View(grammarList);
        }

        [HttpPost]
        public ActionResult DeclareKnowledge(int id)
        {
            var user = this._userManager.GetUser(User.Identity.Name);
            this._grammarManager.DeclareKnowledge(user.ID, id);

            return Json(true);
        }

        [HttpPost]
        public JsonResult GetGrammar(int id)
        {
            var grammar = this._grammarManager.GetGrammar(id);

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de gramatica", "Selecao", id.ToString(), HttpContext.Session.Id);

            return Json(grammar);
        }

        [HttpPost]
        public string GetRequisites(int id)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de gramatica", "Ver requisitos", id.ToString(), HttpContext.Session.Id);

            var grammar = this._grammarManager.GetGrammar(id);

            var hasEnoughLevel = this._grammarManager.HasEnoughLevel(user.JLPT, grammar.Level);

            if (!hasEnoughLevel)
            {
                Response.ContentType = "html";
                return "Conteúdo ainda não disponível.";
            }

            var msg = "Conteúdo não disponível, para tal deve-se primeiramente estudar os seguintes conteúdos:";
            msg += "<ul>";

            foreach (var requisiteId in grammar.Requisites)
            {
                var requisite = this._grammarManager.GetGrammar(requisiteId);
                msg += "<li>" + requisite.Title + "</li>";
            }

            msg += "</ul>";

            Response.ContentType = "html";
            return msg;
        }
    }
}
