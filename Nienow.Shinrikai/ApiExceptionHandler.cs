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

namespace Nienow.Shinrikai
{
    /// <summary>
    /// Converte exceções em ProblemDetails legíveis. O original mostrava a página de erro do ASP.NET
    /// (site.js abria o HTML do erro numa janela); aqui o front recebe title + detail.
    /// </summary>
    public sealed class ApiExceptionHandler(IProblemDetailsService problemDetails, IWebHostEnvironment env, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
        {
            ProblemDetails problem;
            if (exception is DatabaseUnavailableException db)
            {
                logger.LogError(exception, "Banco de dados indisponível: {Message}", db.Message);
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Banco de dados indisponível.",
                    Detail = db.Message,
                };
            }
            else
            {
                logger.LogError(exception, "Erro não tratado em {Path}", context.Request.Path);
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                problem = new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Ocorreu um erro inesperado.",
                    Detail = env.IsDevelopment() ? exception.Message : null,
                };
            }

            return await problemDetails.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = problem,
                Exception = exception,
            });
        }
    }
}
