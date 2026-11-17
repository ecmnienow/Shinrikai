using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Vocabulary
{
    /// <summary>Acesso à tabela SUBJECTS (só SQL, sem regras de negócio).</summary>
    public interface ISubjectRepository
    {
        Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct = default);
        Task<Subject?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default);
        Task<int> CountWordsAsync(int id, CancellationToken ct = default);
        Task AddAsync(Subject subject, CancellationToken ct = default);
        Task UpdateAsync(Subject subject, CancellationToken ct = default);
        Task DeleteAsync(int id, CancellationToken ct = default);
    }

    public sealed class SubjectRepository(IShinrikaiDb db) : ISubjectRepository
    {
        private const string Select = "SELECT SBJ_ID, SBJ_NAME FROM SUBJECTS";

        public Task<IReadOnlyList<Subject>> GetAllAsync(CancellationToken ct = default) =>
            db.QueryAsync($"{Select} ORDER BY SBJ_NAME", Map, ct);

        public Task<Subject?> GetByIdAsync(int id, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync($"{Select} WHERE SBJ_ID = @ID", Map, ct, ("@ID", id));

        /// <summary>Busca sem diferenciar maiúsculas, para não deixar cadastrar "Animais" e "animais".</summary>
        public Task<Subject?> GetByNameAsync(string name, CancellationToken ct = default) =>
            db.QuerySingleOrDefaultAsync($"{Select} WHERE UPPER(SBJ_NAME) = UPPER(@NAME)", Map, ct, ("@NAME", name));

        /// <summary>Quantas palavras usam o assunto em qualquer uma das 3 formas.</summary>
        public async Task<int> CountWordsAsync(int id, CancellationToken ct = default) =>
            Convert.ToInt32(await db.ScalarAsync(
                "SELECT COUNT(*) FROM VOCABULARY WHERE VCB_SBJ_ID_1 = @ID OR VCB_SBJ_ID_2 = @ID OR VCB_SBJ_ID_3 = @ID",
                ct, ("@ID", id)));

        public Task AddAsync(Subject subject, CancellationToken ct = default) =>
            db.ExecuteAsync(
                "INSERT INTO SUBJECTS (SBJ_ID, SBJ_NAME) VALUES (GEN_ID(SUBJECTS_ID_GEN, 1), @NAME)",
                ct, ("@NAME", subject.Name));

        public Task UpdateAsync(Subject subject, CancellationToken ct = default) =>
            db.ExecuteAsync(
                "UPDATE SUBJECTS SET SBJ_NAME = @NAME WHERE SBJ_ID = @ID",
                ct, ("@NAME", subject.Name), ("@ID", subject.Id));

        public Task DeleteAsync(int id, CancellationToken ct = default) =>
            db.ExecuteAsync("DELETE FROM SUBJECTS WHERE SBJ_ID = @ID", ct, ("@ID", id));

        private static Subject Map(DbDataReader r) => new(r.GetInt("SBJ_ID"), r.GetNullableString("SBJ_NAME") ?? "");
    }

    /// <summary>Regras do cadastro de assuntos: validação, nome único e exclusão só de assunto sem palavras.</summary>
    public sealed class SubjectService(ISubjectRepository repository)
    {
        /// <summary>Tamanho da coluna SBJ_NAME (VARCHAR(150)).</summary>
        public const int MaxNameLength = 150;

        public Task<IReadOnlyList<Subject>> ListAsync(CancellationToken ct = default) => repository.GetAllAsync(ct);

        public Task<Subject?> GetAsync(int id, CancellationToken ct = default) => repository.GetByIdAsync(id, ct);

        /// <summary>Validação que não depende do banco (é o que os testes cobrem primeiro).</summary>
        public static Response Validate(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Response.Fail("Informe o nome do assunto.");
            if (name.Trim().Length > MaxNameLength)
                return Response.Fail($"O nome do assunto pode ter no máximo {MaxNameLength} caracteres.");
            return Response.Ok();
        }

        /// <summary><c>subject.Id == 0</c> cadastra; senão altera.</summary>
        public async Task<Response> SaveAsync(Subject subject, CancellationToken ct = default)
        {
            var validation = Validate(subject.Name);
            if (!validation.OK)
                return validation;

            var clean = subject with { Name = subject.Name.Trim() };
            var sameName = await repository.GetByNameAsync(clean.Name, ct);
            if (sameName is not null && sameName.Id != clean.Id)
                return Response.Fail("Já existe um assunto com este nome.");

            return await Response.SaveAsync("Assunto", clean.Id,
                () => repository.AddAsync(clean, ct),
                async () => await repository.GetByIdAsync(clean.Id, ct) is not null,
                () => repository.UpdateAsync(clean, ct));
        }

        public async Task<Response> DeleteAsync(int id, CancellationToken ct = default)
        {
            if (await repository.GetByIdAsync(id, ct) is null)
                return Response.Fail("Assunto não encontrado.");

            var words = await repository.CountWordsAsync(id, ct);
            if (words > 0)
                return Response.Fail($"Este assunto não pode ser excluído: {words} palavra(s) o utilizam.");

            await repository.DeleteAsync(id, ct);
            return Response.Ok("Assunto excluído com sucesso!");
        }
    }
}
