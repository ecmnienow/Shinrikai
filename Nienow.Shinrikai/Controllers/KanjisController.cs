using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Nienow.Helper;
using Nienow.Shinrikai.Kanji;
using Nienow.Shinrikai.Kanji.Model;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Controllers
{
    public class KanjisController : Controller
    {
        #region Managers

        private KanjiManager KanjiMng
        {
            get
            {
                if (Session["KanjiManager"] == null)
                    Session["KanjiManager"] = new KanjiManager();

                return Session["KanjiManager"] as KanjiManager;
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
        public ActionResult Register()
        {
            return View();
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult AddKanji(TKanji kanji, HttpPostedFileBase file)
        {
            if (string.IsNullOrEmpty(kanji.KanjiLetter) || kanji.KanjiLetter.Length != 1 ||
                this.KanjiMng.GetKanji(kanji.KanjiLetter) != null || kanji.ID != 0)
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Kana))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Romaji))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Info))
                return Json(false);

            if (file != null)
                kanji.Image = file.InputStream.ToByteArray();

            this.KanjiMng.AddKanji(kanji);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult GetKanji(string kanjiLetter)
        {
            if (string.IsNullOrWhiteSpace(kanjiLetter))
                throw new ArgumentException();

            var kanji = this.KanjiMng.GetKanji(kanjiLetter);

            if (kanji == null)
                return Json(false);

            return Json(kanji);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public ActionResult UpdateKanji(TKanji kanji, HttpPostedFileBase file)
        {
            if (string.IsNullOrEmpty(kanji.KanjiLetter) || kanji.KanjiLetter.Length != 1 ||
               this.KanjiMng.GetKanji(kanji.KanjiLetter) == null || kanji.ID == 0)
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Kana))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Romaji))
                return Json(false);

            if (string.IsNullOrEmpty(kanji.Info))
                return Json(false);

            if (file != null)
                kanji.Image = file.InputStream.ToByteArray();

            this.KanjiMng.UpdateKanji(kanji);

            return Json(true);
        }

        #endregion

        #region Reading

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de kanjis", "Pagina inicial", null, HttpContext.Session.SessionID);

            return View();
        }

        [HttpPost]
        public ActionResult CheckAnswers(List<GKanjiAnswer> answers)
        {
            if (answers == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = this.UserMng.GetUser(this.User.Identity.Name);

            var result = this.KanjiMng.CheckAnswers(user.ID, answers);

            //Registra interação do usuário
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de kanjis", "Verificar respostas", result.Count(r => !r.Wrong) + " Acertos", HttpContext.Session.SessionID);

            return View("KanjisItems", result);
        }

        [HttpPost]
        public ActionResult GenerateLists(string level, bool mistakes)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de kanjis", "Gerar lista", level + (mistakes ? " mistakes" : ""), HttpContext.Session.SessionID);

            this.KanjiMng.GenerateLists(user.ID, level, mistakes);
            var kanjis = this.KanjiMng.NextPage();
            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public ActionResult NextPage()
        {
            var kanjis = this.KanjiMng.NextPage();

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de Kanjis", "Proxima pagina", this.KanjiMng.CurrentPage.ToString(), HttpContext.Session.SessionID);

            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public ActionResult PreviousPage()
        {
            var kanjis = this.KanjiMng.PreviousPage();

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de Kanjis", "Proxima anterior", this.KanjiMng.CurrentPage.ToString(), HttpContext.Session.SessionID);

            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public ActionResult ReloadList()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Leitura de Kanjis", "Repetir lista", null, HttpContext.Session.SessionID);

            var kanjis = this.KanjiMng.ReloadList();
            return View("KanjisItems", kanjis.Select(t => new GKanjiAnswer(t)).ToList());
        }

        [HttpPost]
        public JsonResult ShowPageStatus()
        {
            var pageStatus = this.KanjiMng.ShowPageStatus();
            return Json(pageStatus);
        }

        #endregion
    }
}
