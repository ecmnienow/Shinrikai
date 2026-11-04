using Nienow.Helper;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.Shinrikai.Core.Adaptive;

/// <summary>
/// Técnica 4.1 da monografia — <b>variação de fragmento</b>.
/// <para>
/// Cada item (palavra, kanji) tem um <see cref="Monitor.Counter"/>. Um erro coloca o contador em
/// <see cref="ResetCounter"/> (10) e o item passa a aparecer no modo "Refazer erradas"
/// (<c>COUNTER &gt; 0</c>); cada acerto o decrementa, e só ao zerar o item conta como sucesso.
/// </para>
/// <para>
/// Portado 1:1 dos <c>ManageMistake</c> originais, inclusive as pequenas diferenças entre
/// módulos (ver <see cref="ApplyTranscription"/> e <see cref="RegisterLackOfKnowledge"/>).
/// </para>
/// </summary>
public static class FragmentVariation
{
    public const int ResetCounter = 10;

    /// <summary>VocabularyManager.ManageMistake — acerto com Counter &lt;= 1 zera e conta sucesso.</summary>
    public static Monitor ApplyVocabulary(Monitor monitor, bool correct, DateTime now) =>
        Apply(monitor, correct, now, successThreshold: 1);

    /// <summary>KanjiManager.ManageMistake — idêntico ao de vocabulário.</summary>
    public static Monitor ApplyKanji(Monitor monitor, bool correct, DateTime now) =>
        Apply(monitor, correct, now, successThreshold: 1);

    /// <summary>
    /// TranscriptionManager.ManageMistake — <b>diferente</b> dos outros dois: o acerto só zera e
    /// conta sucesso quando Counter já é &lt;= 0. Com Counter == 1 o acerto apenas decrementa
    /// para 0, sem incrementar SuccessCounter. Mantido como no original.
    /// </summary>
    public static Monitor ApplyTranscription(Monitor monitor, bool correct, DateTime now) =>
        Apply(monitor, correct, now, successThreshold: 0);

    /// <summary>
    /// TextManager.RegisterLackOfKnowledge — clique na palavra do stretchtext (o aluno não sabia).
    /// Conta falha só se o contador estava exatamente em 0; sempre reinicia em 10.
    /// </summary>
    public static Monitor RegisterLackOfKnowledge(Monitor monitor)
    {
        if (monitor.Counter == 0)
            monitor.FailCounter++;

        monitor.Counter = ResetCounter;
        return monitor;
    }

    /// <summary>ExercisesManager.ManageMistake — exercícios só acumulam sucessos/falhas.</summary>
    public static Monitor ApplyExercise(Monitor monitor, bool correct, DateTime now)
    {
        if (correct)
        {
            monitor.SuccessCounter++;
            monitor.LastHittedTime = now;
        }
        else
        {
            monitor.FailCounter++;
        }
        return monitor;
    }

    private static Monitor Apply(Monitor monitor, bool correct, DateTime now, int successThreshold)
    {
        if (correct)
        {
            if (monitor.Counter <= successThreshold)
            {
                monitor.Counter = 0;
                monitor.SuccessCounter++;
                monitor.LastHittedTime = now;
            }
            else
            {
                monitor.Counter--;
            }
        }
        else
        {
            if (monitor.Counter <= 0)
                monitor.FailCounter++;

            monitor.Counter = ResetCounter;
        }

        return monitor;
    }
}
