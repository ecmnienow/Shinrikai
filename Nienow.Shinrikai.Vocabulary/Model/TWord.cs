namespace Nienow.Shinrikai.Vocabulary.Model;

/// <summary>
/// Item de vocabulário. Até 3 "possibilidades" (significados) por palavra, cada uma com assunto,
/// kanji e tradução(ões) separadas por vírgula. Port de TWord.cs mantendo as regras de correção.
/// </summary>
public class TWord
{
    public int ID { get; set; }

    public string? Subject1 { get; set; }
    public string? Subject2 { get; set; }
    public string? Subject3 { get; set; }
    public int SubjectID1 { get; set; }
    public int SubjectID2 { get; set; }
    public int SubjectID3 { get; set; }

    public string? Kana { get; set; }
    public string? Romaji { get; set; }
    public string? Word1 { get; set; }
    public string? Word2 { get; set; }
    public string? Word3 { get; set; }
    public string? KanjiWord1 { get; set; }
    public string? KanjiWord2 { get; set; }
    public string? KanjiWord3 { get; set; }

    public bool HasImage1 { get; set; }
    public bool HasImage2 { get; set; }
    public bool HasImage3 { get; set; }
    public bool HasAudio1 { get; set; }
    public bool HasAudio2 { get; set; }
    public bool HasAudio3 { get; set; }

    // Blobs só são preenchidos no cadastro; nunca serializados nas listagens.
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Image1 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Image2 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Image3 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Audio1 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Audio2 { get; set; }
    [System.Text.Json.Serialization.JsonIgnore] public byte[]? Audio3 { get; set; }

    public bool CheckTranscriptedWord(string word) => word.Equals(Kana);

    /// <summary>
    /// Igual ao original: comparação exata (sem trim/case), contra o kana (toJapanese) ou contra
    /// qualquer tradução do assunto informado.
    /// </summary>
    public bool CheckTranslatedWord(string? subject, string word, bool toJapanese)
    {
        if (toJapanese)
            return word.Equals(Kana);

        foreach (var option in GetAllTranslatedWords(subject))
        {
            if (string.IsNullOrEmpty(option))
                continue;
            if (word.Equals(option))
                return true;
        }

        return false;
    }

    public bool ContainsKanjiWord(string text) =>
        (!string.IsNullOrEmpty(KanjiWord1) && KanjiWord1.Contains(text)) ||
        (!string.IsNullOrEmpty(KanjiWord2) && KanjiWord2.Contains(text)) ||
        (!string.IsNullOrEmpty(KanjiWord3) && KanjiWord3.Contains(text));

    /// <summary>
    /// Traduções do assunto (ou de todos, se assunto vazio). O original retornava null quando o
    /// assunto não batia com nenhum dos três (o que derrubava a página); aqui retorna vazio.
    /// </summary>
    public string[] GetAllTranslatedWords(string? subject)
    {
        if (string.IsNullOrEmpty(subject))
        {
            var words = new List<string>();
            if (!string.IsNullOrEmpty(Word1)) words.AddRange(Word1.Split(','));
            if (!string.IsNullOrEmpty(Word2)) words.AddRange(Word2.Split(','));
            if (!string.IsNullOrEmpty(Word3)) words.AddRange(Word3.Split(','));
            return words.ToArray();
        }

        if (Subject1 == subject) return Word1?.Split(',') ?? [];
        if (Subject2 == subject) return Word2?.Split(',') ?? [];
        if (Subject3 == subject) return Word3?.Split(',') ?? [];

        return [];
    }

    public bool BelongsTo(string subject) => Subject1 == subject || Subject2 == subject || Subject3 == subject;
}

/// <summary>Item a ser respondido no exercício de tradução.</summary>
public record VocabularyItem(int WordId, string Prompt);

/// <summary>Resposta enviada pelo aluno.</summary>
public record VocabularyAnswer(int WordId, string Prompt, string? Answer);

/// <summary>Resultado da correção (port de GWordAnswer, com a resposta esperada separada).</summary>
public record VocabularyResult(int WordId, string Prompt, string? Answer, bool Wrong, string[] Expected);

public record Subject(int Id, string Name);

/// <summary>
/// Lista gerada para estudo: substitui o VocabularyManager guardado na Session do MVC.
/// As páginas (10 itens) ficam no cliente; a correção é feita no servidor por WordId.
/// </summary>
public record VocabularySession(string Subject, bool ToJapanese, bool Mistakes, IReadOnlyList<IReadOnlyList<VocabularyItem>> Pages)
{
    public int TotalItems => Pages.Sum(p => p.Count);
}
