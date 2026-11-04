using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Nienow.Shinrikai.Vocabulary.Model
{
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
        [JsonIgnore] public byte[]? Image1 { get; set; }
        [JsonIgnore] public byte[]? Image2 { get; set; }
        [JsonIgnore] public byte[]? Image3 { get; set; }
        [JsonIgnore] public byte[]? Audio1 { get; set; }
        [JsonIgnore] public byte[]? Audio2 { get; set; }
        [JsonIgnore] public byte[]? Audio3 { get; set; }

        /// <summary>As três possibilidades, na ordem 1, 2, 3.</summary>
        private (string? Subject, string? Kanji, string? Words)[] Slots =>
            [(Subject1, KanjiWord1, Word1), (Subject2, KanjiWord2, Word2), (Subject3, KanjiWord3, Word3)];

        /// <summary>Possibilidade do assunto (a primeira que bate, como os if/else if do original); sem ela, tudo nulo.</summary>
        private (string? Subject, string? Kanji, string? Words) SlotOf(string subject) =>
            Slots.FirstOrDefault(s => s.Subject == subject);

        public bool CheckTranscriptedWord(string word) => word.Equals(Kana);

        /// <summary>
        /// Igual ao original: comparação exata (sem trim/case), contra o kana (toJapanese) ou contra
        /// qualquer tradução (não vazia) do assunto informado.
        /// </summary>
        public bool CheckTranslatedWord(string? subject, string word, bool toJapanese) =>
            toJapanese ? word.Equals(Kana) : word.Length > 0 && GetAllTranslatedWords(subject).Contains(word);

        /// <summary>Primeira forma em kanji que contém o texto (busca do dicionário), ou null.</summary>
        public string? FirstKanjiWordContaining(string text) =>
            Slots.Select(s => s.Kanji).FirstOrDefault(k => !string.IsNullOrEmpty(k) && k.Contains(text));

        /// <summary>
        /// TWord.GetAllKanjiWords(subject) do original: a forma em kanji da possibilidade daquele
        /// assunto (ou todas, sem assunto). Retorna vazio quando a possibilidade não tem kanji — o
        /// original devolvia [null] e a Transcrição pulava a palavra.
        /// </summary>
        public string[] GetKanjiWords(string? subject)
        {
            var kanji = string.IsNullOrEmpty(subject) ? Slots.Select(s => s.Kanji) : [SlotOf(subject).Kanji];
            return kanji.Where(k => !string.IsNullOrEmpty(k)).Select(k => k!).ToArray();
        }

        /// <summary>
        /// Traduções do assunto (ou de todos, se assunto vazio). O original retornava null quando o
        /// assunto não batia com nenhum dos três (o que derrubava a página); aqui retorna vazio.
        /// </summary>
        public string[] GetAllTranslatedWords(string? subject) =>
            string.IsNullOrEmpty(subject)
                ? Slots.Where(s => !string.IsNullOrEmpty(s.Words)).SelectMany(s => s.Words!.Split(',')).ToArray()
                : SlotOf(subject).Words?.Split(',') ?? [];
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
    public record VocabularySession(string Subject, bool ToJapanese, bool Mistakes, IReadOnlyList<IReadOnlyList<VocabularyItem>> Pages);
}
