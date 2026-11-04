using System;
using System.Collections.Generic;
using System.Linq;
using Nienow.Helper;

namespace Nienow.Shinrikai.Core.Adaptive
{
    /// <summary>
    /// Técnica 4.4 — <b>ordenação adaptativa</b> (exercícios).
    /// Portado de ExercisesManager.ListExercises / ListAllExercises.
    /// </summary>
    public static class AdaptiveOrdering
    {
        /// <summary>Quantidade usada por ListAllExercises.</summary>
        public const int AllExercisesTake = 10;

        /// <summary>
        /// Filtra pelo nível do aluno (HasEnoughLevel não restrito) e embaralha.
        /// <paramref name="take"/> = null equivale a ListExercises; 10 equivale a ListAllExercises.
        /// </summary>
        public static List<int> Select(IEnumerable<(int Id, EJLPT Level)> exercises, EJLPT userLevel, int? take = null, Random? rng = null)
        {
            var allowed = exercises
                .Where(e => JlptLevel.HasEnoughLevel(userLevel, e.Level))
                .Select(e => e.Id)
                .ToList();

            allowed.ShuffleList(rng);

            return take.HasValue ? allowed.Take(take.Value).ToList() : allowed;
        }
    }
}
