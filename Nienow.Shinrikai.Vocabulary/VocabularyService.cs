using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Helper;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Core.Adaptive;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Vocabulary
{
    /// <summary>
    /// Port de VocabularyManager.cs. O original vivia na Session do MVC (páginas, página atual,
    /// assunto atual). Na API isso vira: <see cref="GenerateAsync"/> devolve todas as páginas já
    /// embaralhadas e o cliente navega entre elas; <see cref="CheckAnswersAsync"/> corrige por
    /// WordId e aplica a variação de fragmento (<see cref="FragmentVariation.ApplyVocabulary"/>).
    /// </summary>
    public sealed class VocabularyService(IVocabularyRepository repository, IMonitorStore monitors, TimeProvider? clock = null)
    {
        private DateTime Now => (clock ?? TimeProvider.System).GetLocalNow().DateTime;

        public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default) => repository.GetSubjectsAsync(ct);

        public Task<TWord?> GetWordAsync(int id, CancellationToken ct = default) => repository.GetWordAsync(id, ct);

        public Task<TWord?> GetWordByKanaAsync(string kana, CancellationToken ct = default) => repository.GetWordByKanaAsync(kana, ct);

        public Task<byte[]?> GetMediaAsync(int wordId, VocabularyMedia media, int slot, CancellationToken ct = default) =>
            repository.GetMediaAsync(wordId, media, slot, ct);

        /// <summary>GenerateVocabulary + NextPage do original.</summary>
        public async Task<VocabularySession?> GenerateAsync(int userId, int subjectId, bool toJapanese, bool mistakes, Random? rng = null, CancellationToken ct = default)
        {
            var subject = (await repository.GetSubjectsAsync(ct)).FirstOrDefault(s => s.Id == subjectId);
            if (subject is null)
                return null;

            var words = mistakes
                ? await repository.GetMistakesAsync(userId, subjectId, ct)
                : await repository.GetVocabularyBySubjectAsync(subjectId, ct);

            return BuildSession(subject.Name, words, toJapanese, mistakes, rng ?? Random.Shared);
        }

        /// <summary>
        /// Embaralha e pagina de 10 em 10. O original sorteava índice a índice com um RandomNumber
        /// próprio (semente por milissegundo); o efeito — ordem aleatória, páginas de 10 — é o mesmo.
        /// </summary>
        public static VocabularySession BuildSession(string subject, IEnumerable<TWord> words, bool toJapanese, bool mistakes, Random rng)
        {
            var list = words.ToList();
            list.ShuffleList(rng);
            var pages = list.Select(w => new VocabularyItem(w.ID, PromptFor(w, subject, toJapanese, rng))).Paginate();
            return new VocabularySession(subject, toJapanese, mistakes, pages);
        }

        /// <summary>Japonês → português: mostra o kana. Português → japonês: sorteia uma das traduções do assunto.</summary>
        public static string PromptFor(TWord word, string subject, bool toJapanese, Random rng)
        {
            if (!toJapanese)
                return word.Kana ?? string.Empty;

            var options = word.GetAllTranslatedWords(subject);
            return options.Length == 0 ? string.Empty : options[rng.Next(options.Length)];
        }

        /// <summary>CheckAnswers do original + ManageMistake por palavra.</summary>
        public async Task<IReadOnlyList<VocabularyResult>> CheckAnswersAsync(int userId, string subject, bool toJapanese, IReadOnlyList<VocabularyAnswer> answers, CancellationToken ct = default)
        {
            var words = (await repository.GetWordsAsync(answers.Select(a => a.WordId).ToList(), ct)).ToDictionary(w => w.ID);
            var results = new List<VocabularyResult>(answers.Count);
            foreach (var a in answers)
            {
                if (!words.TryGetValue(a.WordId, out var word))
                {
                    results.Add(new VocabularyResult(a.WordId, a.Prompt, a.Answer, Wrong: true, Expected: []));
                    continue;
                }

                var correct = Check(word, subject, toJapanese, a.Answer);
                await monitors.UpdateAsync(MonitorTables.Vocabulary, userId, word.ID, m => FragmentVariation.ApplyVocabulary(m, correct, Now), ct);
                var expected = correct ? [] : toJapanese ? [word.Kana ?? string.Empty] : word.GetAllTranslatedWords(subject);
                results.Add(new VocabularyResult(word.ID, a.Prompt, a.Answer, !correct, expected));
            }
            return results;
        }

        public static bool Check(TWord word, string subject, bool toJapanese, string? answer) =>
            !string.IsNullOrWhiteSpace(answer) && word.CheckTranslatedWord(subject, answer, toJapanese);

        /// <summary>
        /// Validação do cadastro (VocabularyController.AddWord). A regra original para as possibilidades
        /// 2 e 3 tinha condições contraditórias (na prática a 2 nunca era validada); aqui: se qualquer
        /// campo de uma possibilidade foi preenchido, assunto e tradução dela são obrigatórios.
        /// </summary>
        public static Response ValidateWord(TWord word)
        {
            if (string.IsNullOrWhiteSpace(word.Kana)) return Response.Fail("Informe o kana.");
            if (string.IsNullOrWhiteSpace(word.Romaji)) return Response.Fail("Informe o romaji.");

            (int N, string? Kanji, int SubjectId, string? Translation)[] slots =
            [
                (1, word.KanjiWord1, word.SubjectID1, word.Word1),
                (2, word.KanjiWord2, word.SubjectID2, word.Word2),
                (3, word.KanjiWord3, word.SubjectID3, word.Word3),
            ];
            foreach (var (n, kanji, subjectId, translation) in slots)
            {
                // A possibilidade 1 é sempre obrigatória.
                if (n > 1 && string.IsNullOrWhiteSpace(kanji) && subjectId <= 0 && string.IsNullOrWhiteSpace(translation))
                    continue;
                if (subjectId <= 0) return Response.Fail($"Informe o assunto da possibilidade {n}.");
                if (string.IsNullOrWhiteSpace(translation)) return Response.Fail($"Informe a tradução da possibilidade {n}.");
            }

            return Response.Ok();
        }

        public async Task<Response> SaveWordAsync(TWord word, CancellationToken ct = default)
        {
            var validation = ValidateWord(word);
            if (!validation.OK)
                return validation;

            await (word.ID == 0 ? repository.AddWordAsync(word, ct) : repository.UpdateWordAsync(word, ct));
            return Response.Ok();
        }
    }
}
