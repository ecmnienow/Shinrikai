using Nienow.Shinrikai.Models.Vocabulary;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.AspNetCore.Mvc;

namespace Nienow.Shinrikai.Controllers
{
    public class TranscriptionController : Controller
    {
        public TranscriptionController(MonitoringManager monitoringManager, UserManager userManager, TranscriptionManager transcriptionManager)
        {
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
            this._transcriptionManager = transcriptionManager;
        }

        #region Managers

        private MonitoringManager _monitoringManager;

        public TranscriptionManager _transcriptionManager;

        private UserManager _userManager;

        #endregion

        #region Transcription

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Transcricao de vocabulario", "Pagina inicial", null, HttpContext.Session.Id);

            var mdl = new MVocabulary();
            mdl.Subjects = this._transcriptionManager.Subjects();

            return View(mdl);
        }

        [HttpPost]
        public ActionResult CheckAnswers(List<GWordAnswer> answers)
        {
            if (answers == null)
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);

            var user = this._userManager.GetUser(this.User.Identity.Name);

            var result = this._transcriptionManager.CheckAnswers(user.ID, answers);

            //Registra interação do usuário
            this._monitoringManager.AddMonitor(user.ID, "Transcricao de vocabulario", "Verificar respostas", result.Count(r => !r.Wrong) + " Acertos", HttpContext.Session.Id);

            return View("TranscriptionItems", result);
        }

        [HttpPost]
        public ActionResult GenerateVocabulary(string subject, bool toJapanese, bool mistakes)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Transcricao de vocabulario", "Gerar lista", subject + (mistakes ? " mistakes" : ""), HttpContext.Session.Id);

            this._transcriptionManager.GenerateVocabulary(user.ID, subject, mistakes);
            var vocabulary = this._transcriptionManager.NextPage();
            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult NextPage(bool toJapanese)
        {
            var vocabulary = this._transcriptionManager.NextPage();

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Transcricao de vocabulario", "Proxima pagina", this._transcriptionManager.CurrentPage.ToString(), HttpContext.Session.Id);

            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult PreviousPage(bool toJapanese)
        {
            var vocabulary = this._transcriptionManager.PreviousPage();

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Transcricao de vocabulario", "Proxima anterior", this._transcriptionManager.CurrentPage.ToString(), HttpContext.Session.Id);

            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult ReloadVocabulary(bool toJapanese)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Transcricao de vocabulario", "Repetir lista", null, HttpContext.Session.Id);

            var vocabulary = this._transcriptionManager.ReloadVocabulary();
            return View("TranscriptionItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public JsonResult SearchKanjiWord(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<TWord>());

            var word = this._transcriptionManager.SearchKanjiWord(term).Take(10);
            return Json(word);
        }

        [HttpPost]
        public JsonResult ShowPageStatus()
        {
            var pageStatus = this._transcriptionManager.ShowPageStatus();
            return Json(pageStatus);
        }

        #endregion
    }
}
