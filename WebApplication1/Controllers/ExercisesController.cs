using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Exercises;
using Nienow.Shinrikai.Exercises.Model;
using Nienow.Shinrikai.Grammar;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.Text;
using Nienow.Shinrikai.User;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Nienow.Shinrikai.Controllers
{
    public class ExercisesController : Controller
    {
        public ExercisesController(ExercisesManager exercisesManager, GrammarManager grammarManager, MonitoringManager monitoringManager, TextManager textManager, UserManager userManager)
        {
            this._exercisesManager = exercisesManager;
            this._grammarManager = grammarManager;
            this._monitoringManager = monitoringManager;
            this._textManager = textManager;
            this._userManager = userManager;
        }

        #region Managers

        private ExercisesManager _exercisesManager;

        private GrammarManager _grammarManager;

        private MonitoringManager _monitoringManager;

        private TextManager _textManager;

        private UserManager _userManager;

        #endregion

        #region Register

        [Authorize(Roles = "Administrator, Teacher")]
        //[ValidateInput(false)]
        [HttpPost]
        public JsonResult AddExercise(int id, int grammarId, int textId, string level, string question, int right, string option1, string option2, string option3, string option4, EJLPT jlpt)
        {
            this._exercisesManager.AddExercise(id, grammarId, textId, question, right, option1, option2, option3, option4, jlpt);

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
                    exercise.Level = this._grammarManager.GetGrammar(grammarId).Level;
                if (textId != 0)
                    exercise.Level = this._textManager.GetText(textId).Level;
            }
            else
                exercise = this._exercisesManager.GetExercise(id);

            return View(exercise);
        }

        #endregion

        #region Practice

        public ActionResult Index(int grammarId = 0, int textId = 0)
        {
            if (grammarId > 0 && textId > 0)
                throw new InvalidOperationException();

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            if(grammarId != 0)
                this._monitoringManager.AddMonitor(user.ID, "Resolução de exercícios", "Pagina inicial", "Gramática " + grammarId, HttpContext.Session.Id);
            if(textId != 0)
                this._monitoringManager.AddMonitor(user.ID, "Resolução de exercícios", "Pagina inicial", "Texto " + grammarId, HttpContext.Session.Id);
            if(grammarId == 0 && textId == 0)
                this._monitoringManager.AddMonitor(user.ID, "Resolução de exercícios", "Pagina inicial", "Geral", HttpContext.Session.Id);

            List<int> exercisesList;
            if(grammarId > 0 ^ textId > 0)
                exercisesList = this._exercisesManager.ListExercises(user.ID, user.JLPT, grammarId, textId);
            else
                exercisesList = this._exercisesManager.ListAllExercises(user.ID, user.JLPT);

            return View(exercisesList);
        }

        [HttpPost]
        public ActionResult CheckAnswer(int id, int answer)
        {
            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);

            var result = this._exercisesManager.CheckAnswer(user.ID, id, answer);

            this._monitoringManager.AddMonitor(user.ID, "Resolução de exercícios", "Verifica resposta", "Questao " + id + ": " + result.OK, HttpContext.Session.Id);
            
            return Json(result);
        }

        [HttpPost]
        public JsonResult GetExercise(int id)
        {
            var exercise = this._exercisesManager.GetExercise(id);

            //Registra interação do usuário
            var user = this._userManager.GetUser(User.Identity.Name);
            this._monitoringManager.AddMonitor(user.ID, "Resolução de exercícios", "Selecao", id.ToString(), HttpContext.Session.Id);

            return Json(exercise);
        }

        #endregion
    }
}
