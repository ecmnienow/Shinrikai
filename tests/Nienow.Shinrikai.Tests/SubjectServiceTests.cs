using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;
using Xunit;

namespace Nienow.Shinrikai.Tests
{
    public sealed class SubjectServiceTests
    {
        [Theory]
        [InlineData(null, false)]
        [InlineData("", false)]
        [InlineData("   ", false)]
        [InlineData("Animais", true)]
        public void Validate_requires_a_name(string? name, bool ok) =>
            Assert.Equal(ok, SubjectService.Validate(name).OK);

        [Fact]
        public void Validate_limits_the_name_to_the_column_size()
        {
            Assert.True(SubjectService.Validate(new string('a', SubjectService.MaxNameLength)).OK);
            Assert.False(SubjectService.Validate(new string('a', SubjectService.MaxNameLength + 1)).OK);
        }

        [Fact]
        public async Task Create_trims_the_name_and_adds()
        {
            var repo = new FakeSubjects();
            var response = await new SubjectService(repo).SaveAsync(new Subject(0, "  Cores  "));

            Assert.True(response.OK);
            Assert.Equal("Cores", Assert.Single(repo.Added).Name);
        }

        [Fact]
        public async Task Create_rejects_a_name_already_used_ignoring_case()
        {
            var repo = new FakeSubjects();
            var response = await new SubjectService(repo).SaveAsync(new Subject(0, "animais"));

            Assert.False(response.OK);
            Assert.Empty(repo.Added);
        }

        [Fact]
        public async Task Update_keeps_its_own_name()
        {
            var repo = new FakeSubjects();
            var response = await new SubjectService(repo).SaveAsync(new Subject(1, "Animais"));

            Assert.True(response.OK);
            Assert.Single(repo.Updated);
        }

        [Fact]
        public async Task Update_of_missing_subject_fails()
        {
            var repo = new FakeSubjects();
            Assert.False((await new SubjectService(repo).SaveAsync(new Subject(99, "Novo"))).OK);
            Assert.Empty(repo.Updated);
        }

        [Fact]
        public async Task Delete_refuses_subject_used_by_words()
        {
            var repo = new FakeSubjects();
            var response = await new SubjectService(repo).DeleteAsync(1);

            Assert.False(response.OK);
            Assert.Empty(repo.Deleted);
        }

        [Fact]
        public async Task Delete_removes_unused_subject()
        {
            var repo = new FakeSubjects();
            Assert.True((await new SubjectService(repo).DeleteAsync(2)).OK);
            Assert.Equal([2], repo.Deleted);
        }

        /// <summary>Repositório em memória: 1 = Animais (com 3 palavras), 2 = Comida (sem palavras).</summary>
        private sealed class FakeSubjects : ISubjectRepository
        {
            private readonly List<Subject> subjects = [new(1, "Animais"), new(2, "Comida")];
            private readonly Dictionary<int, int> words = new() { [1] = 3 };

            public List<Subject> Added { get; } = [];
            public List<Subject> Updated { get; } = [];
            public List<int> Deleted { get; } = [];

            public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct = default) =>
                Task.FromResult<IReadOnlyList<Subject>>(subjects);

            public Task<Subject?> GetByIdAsync(int id, CancellationToken ct = default) =>
                Task.FromResult(subjects.FirstOrDefault(s => s.Id == id));

            public Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default) =>
                Task.FromResult(subjects.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)));

            public Task<int> CountWordsAsync(int id, CancellationToken ct = default) =>
                Task.FromResult(words.GetValueOrDefault(id));

            public Task AddAsync(Subject subject, CancellationToken ct = default)
            {
                Added.Add(subject);
                return Task.CompletedTask;
            }

            public Task UpdateAsync(Subject subject, CancellationToken ct = default)
            {
                Updated.Add(subject);
                return Task.CompletedTask;
            }

            public Task DeleteAsync(int id, CancellationToken ct = default)
            {
                Deleted.Add(id);
                return Task.CompletedTask;
            }
        }
    }
}
