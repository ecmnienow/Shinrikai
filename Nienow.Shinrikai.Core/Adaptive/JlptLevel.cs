using System;

namespace Nienow.Shinrikai.Core.Adaptive
{
    /// <summary>
    /// Regra de nível do JLPT (ManagerCore.HasEnoughLevel do original).
    /// <para>
    /// O switch original é equivalente a <c>contentLevel &lt;= userLevel + (restrict ? 0 : 1)</c>
    /// na ordem numérica do enum (None=0, N5=1 … N1=5): sem <c>restrict</c> o aluno enxerga até um
    /// nível acima do seu; com <c>restrict</c>, só o seu nível e os anteriores.
    /// A equivalência é verificada nos testes contra uma cópia literal do switch original.
    /// </para>
    /// </summary>
    public static class JlptLevel
    {
        public static bool HasEnoughLevel(EJLPT userLevel, EJLPT contentLevel, bool restrict = false)
        {
            if (!Enum.IsDefined(userLevel))
                return false;

            var allowance = restrict ? 0 : 1;
            return (int)contentLevel <= (int)userLevel + allowance;
        }

        /// <summary>
        /// Valor gravado nas colunas de nível: "none"/vazio → null; demais → maiúsculas ("n5" → "N5").
        /// O cadastro original de kanjis gravava "n5" e o estudo comparava com "N5".
        /// </summary>
        public static string? Normalize(string? level) =>
            string.IsNullOrWhiteSpace(level) || level.Equals("none", StringComparison.OrdinalIgnoreCase)
                ? null
                : level.Trim().ToUpperInvariant();

        /// <summary>Lê a coluna de nível ("N5", "n4"…); nulo ou inválido vira <paramref name="fallback"/>.</summary>
        public static EJLPT Parse(string? value, EJLPT fallback = EJLPT.None) =>
            Enum.TryParse<EJLPT>(value, true, out var level) && Enum.IsDefined(level) ? level : fallback;
    }
}
