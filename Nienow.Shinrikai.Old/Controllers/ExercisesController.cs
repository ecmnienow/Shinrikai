using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Exercises.Model;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;
using System;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Nienow.Shinrikai.Controllers
{
    public class ExercisesController : Controller
    {
        #region Managers

        private ExercisesManager ExercisesMng
        {
            get
            {
                if (Session["ExercisesMng"] == null)
                    Session["ExercisesMng"] = new ExercisesManager();

                return Session["ExercisesMng"] as ExercisesManager;
            }
        }

        private GrammarManager GrammarMng
        {
            get
            {
                if (Session["GrammarMng"] == null)
                    Session["GrammarMng"] = new GrammarManager();

                return Session["GrammarMng"] as GrammarManager;
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

        private TextManager TextMng
        {
            get
            {
                if (Session["TextMng"] == null)
                    Session["TextMng"] = new TextManager();

                return Session["TextMng"] as TextManager;
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
        public JsonResult AddExercise(int id, int grammarId, int textId, string level, string question, int right, string option1, string option2, string option3, string option4, EJLPT jlpt)
        {
            this.ExercisesMng.AddExercise(id, grammarId, textId, question, right, option1, option2, option3, option4, jlpt);

            return Json(true);
        }

        [Authorize(Roles = "Administrator, Teacher")]
        public ActionResult Register(int id = 0, int grammarId = 0, int textId = 0)
        {
            if (id != 0 && grammarId != 0 && textId != 0)
                throw new InvalidOperationException();

            TExercise exercise;
            if (id == 0)
            {
                exercise = new TExercise() { GrammarId = grammarId, TextId = textId };
                if (grammarId != 0)
                    exercise.Level = this.GrammarMng.GetGrammar(grammarId).Level;
                if (textId != 0)
                    exercise.Level = this.TextMng.GetText(textId).Level;
            }
            else
                exercise = this.ExercisesMng.GetExercise(id);

            return View(exercise);
        }

        #endregion

        #region Practice

        public ActionResult Index(int grammarId = 0, int textId = 0)
        {
            if (grammarId > 0 && textId > 0)
                throw new InvalidOperationException();

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            if(grammarId != 0)
                this.MonitoringMng.AddMonitor(user.ID, "Resolução de exercícios", "Pagina inicial", "Gramática " + grammarId, Session.SessionID);
            if(textId != 0)
                this.MonitoringMng.AddMonitor(user.ID, "Resolução de exercícios", "Pagina inicial", "Texto " + grammarId, Session.SessionID);
            if(grammarId == 0 && textId == 0)
                this.MonitoringMng.AddMonitor(user.ID, "Resolução de exercícios", "Pagina inicial", "Geral", Session.SessionID);

            List<int> exercisesList;
            if(grammarId > 0 ^ textId > 0)
                exercisesList = this.ExercisesMng.ListExercises(user.ID, user.JLPT, grammarId, textId);
            else
                exercisesList = this.ExercisesMng.ListAllExercises(user.ID, user.JLPT);

            return View(exercisesList);
        }

        [HttpPost]
        public ActionResult CheckAnswer(int id, int answer)
        {
            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);

            var result = this.ExercisesMng.CheckAnswer(user.ID, id, answer);

            this.MonitoringMng.AddMonitor(user.ID, "Resolução de exercícios", "Verifica resposta", "Questao " + id + ": " + result.OK, Session.SessionID);
            
            return Json(result);
        }

        [HttpPost]
        public JsonResult GetExercise(int id)
        {
            var exercise = this.ExercisesMng.GetExercise(id);

            //Registra interação do usuário
            var user = this.UserMng.GetUser(User.Identity.Name);
            this.MonitoringMng.AddMonitor(user.ID, "Resolução de exercícios", "Selecao", id.ToString(), Session.SessionID);

            return Json(exercise);
        }

        #endregion
    }
}
