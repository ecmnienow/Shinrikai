using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nienow.Shinrikai.Core
{
    public enum EJLPT
    {
        None = 0,
        N5 = 1,
        N4 = 2,
        N3 = 3,
        N2 = 4,
        N1 = 5
    }

    public enum EMode
    {
        Mistakes,
        Normal
    }

    public enum ESex
    {
        Male = 0,
        Female = 1
    }

    public class Response
    {
        public bool OK { get; set; }
        public string? Message { get; set; }

        public static Response Ok(string? message = null) => new() { OK = true, Message = message };
        public static Response Fail(string message) => new() { OK = false, Message = message };

        /// <summary>
        /// Fluxo comum dos cadastros do original: <c>id == 0</c> inclui; senão altera, se existir.
        /// As mensagens seguem o padrão "{entidade} cadastrado/alterado com sucesso!".
        /// </summary>
        public static async Task<Response> SaveAsync(string entity, int id, Func<Task> add, Func<Task<bool>> exists, Func<Task> update)
        {
            if (id == 0)
            {
                await add();
                return Ok($"{entity} cadastrado com sucesso!");
            }

            if (!await exists())
                return Fail($"{entity} não encontrado.");

            await update();
            return Ok($"{entity} alterado com sucesso!");
        }
    }

    /// <summary>Paginação das listas de estudo (10 itens por página, como no original).</summary>
    public static class Paging
    {
        public const int PageSize = 10;

        public static IReadOnlyList<IReadOnlyList<T>> Paginate<T>(this IEnumerable<T> items) =>
            items.Chunk(PageSize).Select(c => (IReadOnlyList<T>)c).ToList();
    }
}
