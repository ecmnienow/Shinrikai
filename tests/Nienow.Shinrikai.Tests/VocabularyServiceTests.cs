using Nienow.Helper;
using Monitor = Nienow.Helper.Monitor;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Tests;

public class VocabularyServiceTests
{
    private sealed class FakeRepository : IVocabularyRepository
    {
        public List<Subject> Subjects { get; } = [new(1, "Animais"), new(2, "Comida")];
        public List<TWord> Words { get; } = [];
        public Dictionary<(int User, int Word), Monitor> Monitors { get; } = [];
        public int NextMonitorId = 1;

        public Task<IReadOnlyList<Subject>> GetSubjectsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Subject>>(Subjects);
        public Task<IReadOnlyList<TWord>> GetVocabularyBySubjectAsync(int subjectId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TWord>>(Words.Where(w => w.SubjectID1 == subjectId || w.SubjectID2 == subjectId || w.SubjectID3 == subjectId).ToList());
        public Task<IReadOnlyList<TWord>> GetMistakesAsync(int userId, int subjectId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TWord>>(Words
                .Where(w => w.SubjectID1 == subjectId || w.SubjectID2 == subjectId || w.SubjectID3 == subjectId)
                .Where(w => Monitors.TryGetValue((userId, w.ID), out var m) && m.Counter > 0).ToList());
        public Task<IReadOnlyList<TWord>> GetWordsAsync(IReadOnlyList<int> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TWord>>(Words.Where(w => ids.Contains(w.ID)).ToList());
        public Task<TWord?> GetWordAsync(int id, CancellationToken ct = default) => Task.FromResult(Words.FirstOrDefault(w => w.ID == id));
        public Task<TWord?> GetWordByKanaAsync(string kana, CancellationToken ct = default) => Task.FromResult(Words.FirstOrDefault(w => w.Kana == kana));
        public Task<byte[]?> GetMediaAsync(int wordId, VocabularyMedia media, int slot, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);
        public Task AddWordAsync(TWord word, CancellationToken ct = default) { Words.Add(word); return Task.CompletedTask; }
        public Task UpdateWordAsync(TWord word, CancellationToken ct = default) => Task.CompletedTask;
        public Task<Monitor?> GetMonitorAsync(int userId, int wordId, CancellationToken ct = default) =>
            Task.FromResult(Monitors.TryGetValue((userId, wordId), out var m) ? Clone(m) : null);
        public Task SaveMonitorAsync(Monitor monitor, CancellationToken ct = default)
        {
            if (monitor.IsNew) monitor.ID = NextMonitorId++;
            Monitors[(monitor.UsrID, monitor.VcbID)] = Clone(monitor);
            return Task.CompletedTask;
        }
        private static Monitor Clone(Monitor m) => new()
        {
            ID = m.ID, UsrID = m.UsrID, VcbID = m.VcbID, Counter = m.Counter,
            FailCounter = m.FailCounter, SuccessCounter = m.SuccessCounter, LastHittedTime = m.LastHittedTime,
        };
    }

    private static FakeRepository Seed(int count = 25)
    {
        var repo = new FakeRepository();
        for (var i = 1; i <= count; i++)
            repo.Words.Add(new TWord
            {
                ID = i, Kana = $"かな{i}", SubjectID1 = 1, Subject1 = "Animais", Word1 = $"bicho{i},animal{i}",
            });
        repo.Words.Add(new TWord { ID = 100, Kana = "りんご", SubjectID1 = 2, Subject1 = "Comida", Word1 = "maçã" });
        return repo;
    }

    [Fact]
    public async Task Generate_pages_by_ten_with_all_words_of_the_subject()
    {
        var service = new VocabularyService(Seed());

        var session = await service.GenerateAsync(1, subjectId: 1, toJapanese: false, mistakes: false, new Random(1));

        Assert.NotNull(session);
        Assert.Equal("Animais", session.Subject);
        Assert.Equal([10, 10, 5], session.Pages.Select(p => p.Count));
        Assert.Equal(Enumerable.Range(1, 25), session.Pages.SelectMany(p => p).Select(i => i.WordId).Order());
        Assert.All(session.Pages.SelectMany(p => p), i => Assert.Equal($"かな{i.WordId}", i.Prompt));
    }

    [Fact]
    public async Task Generate_to_japanese_prompts_with_a_translation()
    {
        var service = new VocabularyService(Seed(3));
        var session = await service.GenerateAsync(1, 1, toJapanese: true, mistakes: false, new Random(1));

        Assert.All(session!.Pages.SelectMany(p => p),
            i => Assert.Contains(i.Prompt, new[] { $"bicho{i.WordId}", $"animal{i.WordId}" }));
    }

    [Fact]
    public async Task Unknown_subject_returns_null()
    {
        var service = new VocabularyService(Seed());
        Assert.Null(await service.GenerateAsync(1, 999, false, false));
    }

    [Fact]
    public async Task Check_marks_right_and_wrong_and_feeds_the_mistakes_mode()
    {
        var repo = Seed(3);
        var service = new VocabularyService(repo);

        var results = await service.CheckAnswersAsync(userId: 7, "Animais", toJapanese: false,
        [
            new(1, "かな1", "animal1"), // certo (segunda tradução)
            new(2, "かな2", "errado"),
            new(3, "かな3", ""),        // vazio conta como erro
        ]);

        Assert.Equal([false, true, true], results.Select(r => r.Wrong));
        Assert.Equal(["bicho2", "animal2"], results[1].Expected);
        Assert.Empty(results[0].Expected);

        Assert.Equal(10, repo.Monitors[(7, 2)].Counter);
        Assert.Equal(0, repo.Monitors[(7, 1)].Counter);
        Assert.Equal(1, repo.Monitors[(7, 1)].SuccessCounter);

        var mistakes = await service.GenerateAsync(7, 1, false, mistakes: true);
        Assert.Equal([2, 3], mistakes!.Pages.SelectMany(p => p).Select(i => i.WordId).Order());
    }

    [Fact]
    public async Task Check_to_japanese_compares_with_kana_exactly()
    {
        var service = new VocabularyService(Seed(1));
        var results = await service.CheckAnswersAsync(1, "Animais", toJapanese: true, [new(1, "bicho1", "かな1"), new(1, "bicho1", "かな1 ")]);

        Assert.False(results[0].Wrong);
        Assert.True(results[1].Wrong); // comparação exata, como no original
        Assert.Equal(["かな1"], results[1].Expected);
    }

    [Theory]
    [InlineData("", "r", 1, "w", false)]
    [InlineData("k", "", 1, "w", false)]
    [InlineData("k", "r", 0, "w", false)]
    [InlineData("k", "r", 1, "", false)]
    [InlineData("k", "r", 1, "w", true)]
    public void Validate_word_required_fields(string kana, string romaji, int subject, string word, bool ok) =>
        Assert.Equal(ok, VocabularyService.ValidateWord(new TWord { Kana = kana, Romaji = romaji, SubjectID1 = subject, Word1 = word }).OK);

    [Fact]
    public void Validate_word_optional_slot_requires_subject_and_translation_when_used()
    {
        var word = new TWord { Kana = "k", Romaji = "r", SubjectID1 = 1, Word1 = "w", KanjiWord2 = "字" };
        Assert.False(VocabularyService.ValidateWord(word).OK);

        word.SubjectID2 = 2;
        word.Word2 = "letra";
        Assert.True(VocabularyService.ValidateWord(word).OK);
    }

    [Fact]
    public void User_validation()
    {
        var user = new TUser { Name = "A", Email = "a@b.com", Login = "a", Password = "123", RepeatPassword = "123" };
        Assert.True(UserService.Validate(user).OK);

        user.RepeatPassword = "456";
        Assert.False(UserService.Validate(user).OK);

        user.RepeatPassword = "123";
        user.Email = "invalido";
        Assert.False(UserService.Validate(user).OK);
    }

    [Theory]
    [InlineData("A", Roles.Administrator)]
    [InlineData("T", Roles.Teacher)]
    [InlineData("S", Roles.Student)]
    [InlineData(null, Roles.Student)]
    public void Roles_from_user_type(string? type, string role) => Assert.Equal(role, Roles.FromUserType(type));
}
