using Nienow.Shinrikai.Models.Vocabulary;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Nienow.Shinrikai.Controllers
{
    public class TranscriptionController : Controller
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

        public TranscriptionManager TranscriptionMng
        {
            get
            {
                if (Session["TranscriptionManager"] == null)
                    Session["TranscriptionManager"] = new TranscriptionManager();

                return Session["TranscriptionManager"] as TranscriptionManager;
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

        #region Transcription

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Transcricao de vocabulario", "Pagina inicial", null, Session.SessionID);

            var mdl = new MVocabulary();
            mdl.Subjects = this.TranscriptionMng.Subjects();

            return View(mdl);
        }

        [HttpPost]
        public ActionResult CheckAnswers(List<GWordAnswer> answers)
        {
            if (answers == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = this.UserMng.GetUser(this.User.Identity.Name);

            var result = this.TranscriptionMng.CheckAnswers(user.ID, answers);

            //Registra interação do usuário
            this.MonitoringMng.AddMonitor(user.ID, "Transcricao de vocabulario", "Verificar respostas", result.Count(r => !r.Wrong) + " Acertos", Session.SessionID);

            return View("TranscriptionItems", result);
        }

        [HttpPost]
        public ActionResult GenerateVocabulary(string subject, bool toJapanese, bool mistakes)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Transcricao de vocabulario", "Gerar lista", subject + (mistakes ? " mistakes" : ""), Session.SessionID);

            this.TranscriptionMng.GenerateVocabulary(user.ID, subject, mistakes);
            var vocabulary = this.TranscriptionMng.NextPage();
            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult NextPage(bool toJapanese)
        {
            var vocabulary = this.TranscriptionMng.NextPage();
            
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Transcricao de vocabulario", "Proxima pagina", this.TranscriptionMng.CurrentPage.ToString(), Session.SessionID);

            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult PreviousPage(bool toJapanese)
        {
            var vocabulary = this.TranscriptionMng.PreviousPage();

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Transcricao de vocabulario", "Proxima anterior", this.TranscriptionMng.CurrentPage.ToString(), Session.SessionID);

            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult ReloadVocabulary(bool toJapanese)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Transcricao de vocabulario", "Repetir lista", null, Session.SessionID);

            var vocabulary = this.TranscriptionMng.ReloadVocabulary();
            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }
        
        [HttpPost]
        public JsonResult SearchKanjiWord(string term)
        {
            if(string.IsNullOrWhiteSpace(term))
                return Json(new List<TWord>());

            var word = this.TranscriptionMng.SearchKanjiWord(term).Take(10);
            return Json(word);
        }

        [HttpPost]
        public JsonResult ShowPageStatus()
        {
            var pageStatus = this.TranscriptionMng.ShowPageStatus();
            return Json(pageStatus);
        }

        #endregion
    }
}
