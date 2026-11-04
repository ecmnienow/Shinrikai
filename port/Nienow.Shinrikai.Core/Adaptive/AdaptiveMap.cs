namespace Nienow.Shinrikai.Core.Adaptive;

/// <summary>
/// Técnica 4.2 — <b>mapa adaptativo</b> (gramática).
/// Portado de GrammarManager.ListGrammars + DBGrammar.HasKnowledge.
/// </summary>
public static class AdaptiveMap
{
    /// <summary>Tempo de leitura após o qual o front declara o conhecimento (Views/Grammar/Index.cshtml).</summary>
    public static readonly TimeSpan ReadingTimeToDeclareKnowledge = TimeSpan.FromSeconds(20);

    /// <summary>
    /// Converte GRM_REQUISITES ("001;002;010") nos IDs. O original fazia <c>TrimStart('0')</c> em
    /// cada item e concatenava no SQL; aqui viram inteiros (vão como parâmetros).
    /// Itens vazios/inválidos são ignorados.
    /// </summary>
    public static IReadOnlyList<int> ParseRequisites(string? requisites)
    {
        if (string.IsNullOrEmpty(requisites))
            return Array.Empty<int>();

        var ids = new List<int>();
        foreach (var r in requisites.Split(';'))
            if (int.TryParse(r.Trim(), out var id))
                ids.Add(id);

        return ids;
    }

    /// <summary>
    /// DBGrammar.HasKnowledge: sem pré-requisitos → true; senão, o COUNT(*) em GRA_MONITOR do
    /// usuário para esses pré-requisitos tem de ser igual ao número de pré-requisitos.
    /// </summary>
    /// <param name="requisitesString">Valor cru de GRM_REQUISITES.</param>
    /// <param name="declaredCount">COUNT(*) em GRA_MONITOR para os pré-requisitos e o usuário.</param>
    public static bool HasKnowledge(string? requisitesString, int declaredCount)
    {
        if (string.IsNullOrEmpty(requisitesString))
            return true;

        // O original compara com requisites.Length (contagem bruta do Split).
        return declaredCount == requisitesString.Split(';').Length;
    }

    /// <summary>
    /// Regra de liberação de um tópico (GrammarManager.ListGrammars): liberado se o conteúdo está
    /// no nível do aluno (restrito), ou se ele já cumpriu os pré-requisitos e o conteúdo está até
    /// um nível acima.
    /// </summary>
    public static bool IsUnlocked(EJLPT userLevel, EJLPT contentLevel, bool hasKnowledge) =>
        JlptLevel.HasEnoughLevel(userLevel, contentLevel, restrict: true) ||
        (hasKnowledge && JlptLevel.HasEnoughLevel(userLevel, contentLevel));
}
