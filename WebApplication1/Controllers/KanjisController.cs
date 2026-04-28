using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Nienow.Helper;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Kanji.Model;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;

namespace Nienow.Shinrikai.Controllers
{
    public class KanjisController : Controller
    {
        public KanjisController(MonitoringManager monitoringManager, UserManager userManager, KanjiManager kanjiManager)
        {
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
            this._kanjiManager = kanjiManager;
        }

        #region Managers

        private MonitoringManager _monitoringManager;

        private UserManager _userManager;

        private KanjiManager _kanjiManager;

        #endregion

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register()
        {
            return View();
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult AddKanji(TKanji kanji, IFormFile file)
        {
            if (string.IsNullOrEmpty(kanji.KanjiLetter) || kanji.KanjiLetter.Length != 1 ||
                this._kanjiManager.GetKanji(kanji.KanjiLetter) != null || kanji.ID != 0)
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Kana))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Romaji))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Info))
                return Json(false);

            if (file != null && file.Length > 0)
                kanji.Image = file.OpenReadStream().ToByteArray();

            this._kanjiManager.AddKanji(kanji);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult GetKanji(string kanjiLetter)
        {
            if (string.IsNullOrWhiteSpace(kanjiLetter))
                throw new ArgumentException();

            var kanji = this._kanjiManager.GetKanji(kanjiLetter);

            if (kanji == null)
                return Json(false);

            return Json(kanji);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public ActionResult UpdateKanji(TKanji kanji, IFormFile file)
        {
            if (string.IsNullOrEmpty(kanji.KanjiLetter) || kanji.KanjiLetter.Length != 1 ||
               this._kanjiManager.GetKanji(kanji.KanjiLetter) == null || kanji.ID == 0)
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Kana))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Romaji))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Info))
                return Json(false);

            if (file != null && file.Length > 0)
                kanji.Image = file.OpenReadStream().ToByteArray();

            this._kanjiManager.UpdateKanji(kanji);

            return Json(true);
        }

        #endregion

        #region Reading

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de kanjis", "Pagina inicial", null, HttpContext.Session.Id);

            return View();
        }

        [HttpPost]
        public ActionResult CheckAnswers(List<GKanjiAnswer> answers)
        {
            if (answers == null)
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);

            var user = this._userManager.GetUser(this.User.Identity.Name);

            var result = this._kanjiManager.CheckAnswers(user.ID, answers);

            //Registra interação do usuário
            this._monitoringManager.AddMonitor(user.ID, "Leitura de kanjis", "Verificar respostas", result.Count(r => !r.Wrong) + " Acertos", HttpContext.Session.Id);

            return View("KanjisItems", result);
        }

        [HttpPost]
        public ActionResult GenerateLists(string level, bool mistakes)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de kanjis", "Gerar lista", level + (mistakes ? " mistakes" : ""), HttpContext.Session.Id);

            this._kanjiManager.GenerateLists(user.ID, level, mistakes);
            var kanjis = this._kanjiManager.NextPage();
            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public ActionResult NextPage()
        {
            var kanjis = this._kanjiManager.NextPage();

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de Kanjis", "Proxima pagina", this._kanjiManager.CurrentPage.ToString(), HttpContext.Session.Id);

            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public ActionResult PreviousPage()
        {
            var kanjis = this._kanjiManager.PreviousPage();

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de Kanjis", "Proxima anterior", this._kanjiManager.CurrentPage.ToString(), HttpContext.Session.Id);

            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public ActionResult ReloadList()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Leitura de Kanjis", "Repetir lista", null, HttpContext.Session.Id);

            var kanjis = this._kanjiManager.ReloadList();
            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public JsonResult ShowPageStatus()
        {
            var pageStatus = this._kanjiManager.ShowPageStatus();
            return Json(pageStatus);
        }

        #endregion
    }
}
