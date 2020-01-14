using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Exercises.DB;
using Nienow.Shinrikai.Exercises.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nienow.Shinrikai.Exercises
{
    public class ExercisesManager : ManagerCore
    {
        #region Private Fields

        private readonly DBExercise _db = new DBExercise();

        #endregion

        #region Private Methods

        private void ManageMistake(int userId, TExercise exercise, bool result)
        {
            using (this._db.Connect())
            {
                var monitor = this._db.GetMonitor(userId, exercise.ID);

                if (monitor == null)
                    monitor = new Monitor() { UsrID = userId, VcbID = exercise.ID, };

                if (result)
                {
                    monitor.SuccessCounter++;
                    monitor.LastHittedTime = DateTime.Now;
                }
                else
                    monitor.FailCounter++;

                if (monitor.ID == -1)
                    this._db.AddMonitor(monitor);
                else
                    this._db.UpdateMonitor(monitor);
            }
        }

        #endregion

        #region Public Methods

        public void AddExercise(int id, int grammarId, int textId, string question, int right, string option1, string option2, string option3, string option4, EJLPT jlpt)
        {
            using (this._db.Connect())
                if (id == 0)
                    this._db.AddExercise(grammarId, textId, question, right, option1, option2, option3, option4, jlpt);
                else
                    this._db.UpdateExercise(id, grammarId, textId, question, right, option1, option2, option3, option4, jlpt);
        }

        public Response CheckAnswer(int userId, int exerciseId, int answer)
        {
            var exercise = this.GetExercise(exerciseId);

            var result = exercise.RightAnswer == answer;

            this.ManageMistake(userId, exercise, result);

            if (result)
                return new Response() { OK = true };
            else
                return new Response()
                           {
                               OK = false,
                               Message = string.Format("A resposta correta era: \"{0}\"", exercise.RightAnswer)
                           };
        }

        public TExercise GetExercise(int id)
        {
            using (this._db.Connect())
                return this._db.GetExercise(id);
        }

        public List<int> ListAllExercises(int userId, EJLPT jlpt)
        {
            using (this._db.Connect())
            {
                var exercises = this._db.ListAllExercises(userId);

                var allowed = new List<int>();

                foreach (var exercise in exercises)
                {
                    var exerciseInfo = this._db.GetExercise(exercise);
                    if (this.HasEnoughLevel(jlpt, exerciseInfo.Level))
                        allowed.Add(exercise);
                }

                allowed = allowed.ShuffleList().Take(10).ToList();

                return allowed;
            }
        }

        public List<int> ListExercises(int userId, EJLPT jlpt, int grammarId, int textId)
        {
            using (this._db.Connect())
            {
                var exercises = this._db.ListExercises(userId, grammarId, textId);

                var allowed = new List<int>();

                foreach (var exercise in exercises)
                {
                    var exerciseInfo = this._db.GetExercise(exercise);
                    if (this.HasEnoughLevel(jlpt, exerciseInfo.Level))
                        allowed.Add(exercise);
                }

                allowed.ShuffleList();

                return allowed;
            }
        }

        #endregion
    }
}
