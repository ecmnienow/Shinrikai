using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nienow.DataBaseConn;

namespace Nienow.Shinrikai.Api
{
    /// <summary>
    /// Converte exceções em ProblemDetails legíveis. O original mostrava a página de erro do ASP.NET
    /// (site.js abria o HTML do erro numa janela); aqui o front recebe title + detail.
    /// </summary>
    public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, IWebHostEnvironment env, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
        {
            var unavailable = exception is DatabaseUnavailableException;
            logger.LogError(exception, unavailable ? "Banco de dados indisponível" : "Erro não tratado em {Path}", context.Request.Path);

            var status = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
            context.Response.StatusCode = status;
            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = status,
                    Title = unavailable ? "Banco de dados indisponível." : "Ocorreu um erro inesperado.",
                    Detail = unavailable || env.IsDevelopment() ? exception.Message : null,
                },
            });
        }
    }
}
