using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nienow.Shinrikai.Core;
using Nienow.Shinrikai.Monitoring;
using Nienow.Shinrikai.User;
using Nienow.Shinrikai.Vocabulary;
using Nienow.Shinrikai.Vocabulary.Model;

namespace Nienow.Shinrikai.Api.Controllers
{
    /// <summary>Corpo de POST e PUT: só o nome (o id vem da rota).</summary>
    public sealed record SubjectRequest(string? Name);

    /// <summary>Cadastro de assuntos do vocabulário (Administrator, Teacher).</summary>
    [Route("api/subjects")]
    [Authorize(Roles = Roles.ContentEditors)]
    public sealed class SubjectsController(SubjectService subjects, IMonitoringService monitoring)
        : ShinrikaiController(monitoring, "Cadastro de assuntos")
    {
        [HttpGet]
        public Task<IReadOnlyList<Subject>> List(CancellationToken ct) => subjects.ListAsync(ct);

        [HttpGet("{id:int}")]
        public async Task<ActionResult<Subject>> Get(int id, CancellationToken ct) =>
            await subjects.GetAsync(id, ct) is { } subject ? subject : NotFound(Core.Response.Fail("Assunto não encontrado."));

        [HttpPost]
        public async Task<ActionResult<Response>> Create(SubjectRequest request, CancellationToken ct)
        {
            var response = await subjects.SaveAsync(new Subject(0, request.Name ?? ""), ct);
            if (response.OK)
                await Log("Cadastrar", request.Name, ct);
            return Result(response);
        }

        [HttpPut("{id:int}")]
        public async Task<ActionResult<Response>> Update(int id, SubjectRequest request, CancellationToken ct)
        {
            if (id <= 0)
                return BadRequest(Core.Response.Fail("Assunto inválido."));

            var response = await subjects.SaveAsync(new Subject(id, request.Name ?? ""), ct);
            if (response.OK)
                await Log("Alterar", $"{id} {request.Name}", ct);
            return Result(response);
        }

        [HttpDelete("{id:int}")]
        public async Task<ActionResult<Response>> Delete(int id, CancellationToken ct)
        {
            var response = await subjects.DeleteAsync(id, ct);
            if (response.OK)
                await Log("Excluir", id.ToString(), ct);
            return Result(response);
        }
    }
}
