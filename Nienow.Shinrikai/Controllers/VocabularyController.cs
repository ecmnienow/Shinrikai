using System.Net;
using Nienow.Helper;
using Nienow.Shinrikai.Models.Vocabulary;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Nienow.Shinrikai.Dictionary;
using Microsoft.AspNetCore.Http;

namespace Nienow.Shinrikai.Controllers
{
    public class VocabularyController : Controller
    {
        public VocabularyController(MonitoringManager monitoringManager, UserManager userManager, VocabularyManager vocabularyManager)
        {
            this._monitoringManager = monitoringManager;
            this._userManager = userManager;
            this._vocabularyManager = vocabularyManager;
        }

        #region Managers

        private MonitoringManager _monitoringManager;

        private UserManager _userManager;

        private VocabularyManager _vocabularyManager;

        #endregion

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0)
        {
            var mdl = new MVocabulary();
            mdl.Subjects = this._vocabularyManager.Subjects();
            if (id != 0)
                mdl.Word = this._vocabularyManager.GetWord(id);

            return View(mdl);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult AddWord(TWord word, IFormFile file1, IFormFile file2, IFormFile file3, IFormFile file4, IFormFile file5, IFormFile file6)
        {
            if (string.IsNullOrWhiteSpace(word.Kana)) return Json(false);
            if (string.IsNullOrWhiteSpace(word.Romaji)) return Json(false);

            //Possibilidade 1
            if (word.SubjectID1 <= 0) return Json(false);
            if (string.IsNullOrWhiteSpace(word.Word1)) return Json(false);

            if ((!string.IsNullOrWhiteSpace(word.KanjiWord2) || !(word.SubjectID2 > 0) || !string.IsNullOrWhiteSpace(word.Word2)) &&
                (!string.IsNullOrWhiteSpace(word.KanjiWord2) && !(word.SubjectID2 <= 0) && !string.IsNullOrWhiteSpace(word.Word2)))
            {
                if (word.SubjectID2 <= 0) return Json(false);
                if (string.IsNullOrWhiteSpace(word.Word2)) return Json(false);
            }

            if ((!string.IsNullOrWhiteSpace(word.KanjiWord3) || !(word.SubjectID3 > 0) || !string.IsNullOrWhiteSpace(word.Word3)) &&
                (!string.IsNullOrWhiteSpace(word.KanjiWord3) && !(word.SubjectID3 > 0) && !string.IsNullOrWhiteSpace(word.Word3)))
            {
                if (word.SubjectID3 <= 0) return Json(false);
                if (string.IsNullOrWhiteSpace(word.Word3)) return Json(false);
            }

            if (file1 != null)
                word.Image1 = file1.OpenReadStream().ToByteArray();
            if (file2 != null)
                word.Image2 = file2.OpenReadStream().ToByteArray();
            if (file3 != null)
                word.Image3 = file3.OpenReadStream().ToByteArray();
            if (file4 != null)
                word.Audio1 = file4.OpenReadStream().ToByteArray();
            if (file5 != null)
                word.Audio2 = file5.OpenReadStream().ToByteArray();
            if (file6 != null)
                word.Audio3 = file6.OpenReadStream().ToByteArray();

            if (word.ID == 0)
                this._vocabularyManager.AddWord(word);
            else
                this._vocabularyManager.UpdateWord(word);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult GetWord(string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return Json(false);

            var word = this._vocabularyManager.GetWord(search);

            if (word == null)
                return Json(false);

            return Json(word);
        }

        #endregion

        #region Translation

        #region Private Methods

        //private void ApplyCorrectionToCurrentLanguage(TextBox textBox, int currentIndex)
        //{
        //    if (currentIndex >= this.CurrentVocabularyList.Count)
        //        return;

        //    bool result = true;

        //    var word = this.CurrentVocabularyList[currentIndex];

        //    if (!word.CheckTranslatedWord(this.cbxSubject.Text, textBox.Text))
        //    {
        //        textBox.Text += string.Format(" ( {0} )", word.GetCurrentLanguageWord(this.cbxSubject.Text).Replace(",", ", "));
        //        textBox.ForeColor = OptionsManager.Instance.Options.CorrectionColor;

        //        this._allCorrect = false;
        //        result = false;
        //    }

        //    this.VocabylaryMng.ManageMistake(this.CurrentVocabularyList[currentIndex], result);
        //}

        //private void ApplyCorrectionToJapanese(Label label, TextBox textBox, int currentIndex)
        //{
        //    if (!label.Visible)
        //        return;

        //    string correctWords = this.VocabylaryMng.CheckJapaneseAnswer(label.Text, textBox.Text, this.cbxSubject.Text);

        //    if (string.IsNullOrWhiteSpace(correctWords))
        //    {
        //        this.VocabylaryMng.ManageMistake(this.CurrentVocabularyList[currentIndex], true);
        //        return;
        //    }

        //    this.VocabylaryMng.ManageMistake(this.CurrentVocabularyList[currentIndex], false);
        //    textBox.Text += string.Format(" ( {0} )", correctWords);
        //    textBox.ForeColor = Color.Red;
        //    this._allCorrect = false;
        //}

        //private void CheckTranslationResults()
        //{
        //    if (rbtJapaneseToCurrentLanguage.Checked)
        //    {
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer1, 0);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer2, 1);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer3, 2);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer4, 3);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer5, 4);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer6, 5);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer7, 6);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer8, 7);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer9, 8);
        //        this.ApplyCorrectionToCurrentLanguage(this.txtAnswer10, 9);
        //    }
        //    else
        //        if (rbtCurrentLanguageToJapanese.Checked)
        //        {
        //            this.ApplyCorrectionToJapanese(this.lblQuestion1, this.txtAnswer1, 0);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion2, this.txtAnswer2, 1);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion3, this.txtAnswer3, 2);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion4, this.txtAnswer4, 3);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion5, this.txtAnswer5, 4);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion6, this.txtAnswer6, 5);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion7, this.txtAnswer7, 6);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion8, this.txtAnswer8, 7);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion9, this.txtAnswer9, 8);
        //            this.ApplyCorrectionToJapanese(this.lblQuestion10, this.txtAnswer10, 9);
        //        }

        //    UserManager.Instance.UserCollection.Save();

        //    if (this._allCorrect)
        //        MessageBox.Show(
        //            text: this._congratsText,
        //            caption: this._congratsTitle,
        //            buttons: MessageBoxButtons.OK,
        //            icon: MessageBoxIcon.Information);

        //    this.btnCheck.Enabled = false;

        //    this._allCorrect = true;
        //    this.ManageUI();
        //}

        #endregion

        public ActionResult Index()
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de vocabulario", "Pagina inicial", null, HttpContext.Session.Id);

            var mdl = new MVocabulary();
            mdl.Subjects = this._vocabularyManager.Subjects();

            return View(mdl);
        }

        [HttpPost]
        public ActionResult CheckAnswers(List<GWordAnswer> answers, bool toJapanese)
        {
            if (answers == null)
                return new StatusCodeResult((int)HttpStatusCode.BadRequest);

            var user = this._userManager.GetUser(this.User.Identity.Name);

            var result = this._vocabularyManager.CheckAnswers(user.ID, answers, toJapanese);

            //Registra interação do usuário
            this._monitoringManager.AddMonitor(user.ID, "Estudo de vocabulario", "Verificar respostas", result.Count(r => !r.Wrong) + " Acertos", HttpContext.Session.Id);

            return View("TranslationItems", result);
        }

        [HttpPost]
        public ActionResult GenerateVocabulary(string subject, bool toJapanese, bool mistakes)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de vocabulario", "Gerar lista", subject + (mistakes ? " mistakes" : ""), HttpContext.Session.Id);

            this._vocabularyManager.GenerateVocabulary(user.ID, subject, mistakes);
            var vocabulary = this._vocabularyManager.NextPage(toJapanese);
            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult NextPage(bool toJapanese)
        {
            var vocabulary = this._vocabularyManager.NextPage(toJapanese);

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de vocabulario", "Proxima pagina", this._vocabularyManager.CurrentPage.ToString(), HttpContext.Session.Id);

            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult PreviousPage(bool toJapanese)
        {
            var vocabulary = this._vocabularyManager.PreviousPage(toJapanese);

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de vocabulario", "Proxima anterior", this._vocabularyManager.CurrentPage.ToString(), HttpContext.Session.Id);

            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult ReloadVocabulary(bool toJapanese)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Estudo de vocabulario", "Repetir lista", null, HttpContext.Session.Id);

            var vocabulary = this._vocabularyManager.ReloadVocabulary(toJapanese);
            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public JsonResult ShowPageStatus()
        {
            var pageStatus = this._vocabularyManager.ShowPageStatus();
            return Json(pageStatus);
        }

        #endregion
    }
}
