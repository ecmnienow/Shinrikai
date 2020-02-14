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
using System.Web.Mvc;

namespace Nienow.Shinrikai.Controllers
{
    public class VocabularyController : Controller
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

        private UserManager UserMng
        {
            get
            {
                if (Session["UserManager"] == null)
                    Session["UserManager"] = new UserManager();

                return Session["UserManager"] as UserManager;
            }
        }

        private VocabularyManager VocabylaryMng
        {
            get
            {
                if (Session["VocabularyManager"] == null)
                    Session["VocabularyManager"] = new VocabularyManager();

                return Session["VocabularyManager"] as VocabularyManager;
            }
        }

        #endregion

        #region Register
        
        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0)
        {
            var mdl = new MVocabulary();
            mdl.Subjects = this.VocabylaryMng.Subjects();
            if(id != 0)
                mdl.Word = this.VocabylaryMng.GetWord(id);

            return View(mdl);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult AddWord(TWord word, HttpPostedFileBase file1, HttpPostedFileBase file2, HttpPostedFileBase file3, HttpPostedFileBase file4, HttpPostedFileBase file5, HttpPostedFileBase file6)
        {
            if (string.IsNullOrWhiteSpace(word.Kana)) return Json(false);
            if (string.IsNullOrWhiteSpace(word.Romaji)) return Json(false);

            //Possibilidade 1
            if (word.SubjectID1 <= 0) return Json(false);
            if (string.IsNullOrWhiteSpace(word.Word1)) return Json(false);

            if ((!string.IsNullOrWhiteSpace(word.KanjiWord2) || !(word.SubjectID2 > 0) || !string.IsNullOrWhiteSpace(word.Word2)) &&
                (!string.IsNullOrWhiteSpace(word.KanjiWord2) && !(word.SubjectID2 <= 0) && !string.IsNullOrWhiteSpace(word.Word2))) {
                if (word.SubjectID2 <= 0) return Json(false);
                if (string.IsNullOrWhiteSpace(word.Word2)) return Json(false);
            }
            
            if ((!string.IsNullOrWhiteSpace(word.KanjiWord3) || !(word.SubjectID3 > 0) || !string.IsNullOrWhiteSpace(word.Word3)) &&
                (!string.IsNullOrWhiteSpace(word.KanjiWord3) && !(word.SubjectID3 > 0) && !string.IsNullOrWhiteSpace(word.Word3))) {
                if (word.SubjectID3 <= 0) return Json(false);
                if (string.IsNullOrWhiteSpace(word.Word3)) return Json(false);
            }
            
            if(file1 != null)
                word.Image1 = file1.InputStream.ToByteArray();
            if(file2 != null)
                word.Image2 = file2.InputStream.ToByteArray();
            if(file3 != null)
                word.Image3 = file3.InputStream.ToByteArray();
            if(file4 != null)
                word.Audio1 = file4.InputStream.ToByteArray();
            if(file5 != null)
                word.Audio2 = file5.InputStream.ToByteArray();
            if(file6 != null)
                word.Audio3 = file6.InputStream.ToByteArray();

            if(word.ID == 0)
                this.VocabylaryMng.AddWord(word);
            else
                this.VocabylaryMng.UpdateWord(word);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        [HttpPost]
        public JsonResult GetWord(string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return Json(false);

            var word = this.VocabylaryMng.GetWord(search);

            if(word == null)
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
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de vocabulario", "Pagina inicial", null, HttpContext.Session.SessionID);

            var mdl = new MVocabulary();
            mdl.Subjects = this.VocabylaryMng.Subjects();

            return View(mdl);
        }

        [HttpPost]
        public ActionResult CheckAnswers(List<GWordAnswer> answers, bool toJapanese)
        {
            if (answers == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var user = this.UserMng.GetUser(this.User.Identity.Name);

            var result = this.VocabylaryMng.CheckAnswers(user.ID, answers, toJapanese);

            //Registra interação do usuário
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de vocabulario", "Verificar respostas", result.Count(r => !r.Wrong) + " Acertos", HttpContext.Session.SessionID);

            return View("TranslationItems", result);
        }

        [HttpPost]
        public ActionResult GenerateVocabulary(string subject, bool toJapanese, bool mistakes)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de vocabulario", "Gerar lista", subject + (mistakes ? " mistakes" : ""), HttpContext.Session.SessionID);

            this.VocabylaryMng.GenerateVocabulary(user.ID, subject, mistakes);
            var vocabulary = this.VocabylaryMng.NextPage(toJapanese);
            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult NextPage(bool toJapanese)
        {
            var vocabulary = this.VocabylaryMng.NextPage(toJapanese);
            
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de vocabulario", "Proxima pagina", this.VocabylaryMng.CurrentPage.ToString(), HttpContext.Session.SessionID);

            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult PreviousPage(bool toJapanese)
        {
            var vocabulary = this.VocabylaryMng.PreviousPage(toJapanese);

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de vocabulario", "Proxima anterior", this.VocabylaryMng.CurrentPage.ToString(), HttpContext.Session.SessionID);

            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public ActionResult ReloadVocabulary(bool toJapanese)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Estudo de vocabulario", "Repetir lista", null, HttpContext.Session.SessionID);

            var vocabulary = this.VocabylaryMng.ReloadVocabulary(toJapanese);
            return View("TranslationItems", vocabulary.Select(t => new GWordAnswer() { Word = t, Answer = string.Empty }).ToList());
        }

        [HttpPost]
        public JsonResult ShowPageStatus()
        {
            var pageStatus = this.VocabylaryMng.ShowPageStatus();
            return Json(pageStatus);
        }

        #endregion
    }
}
