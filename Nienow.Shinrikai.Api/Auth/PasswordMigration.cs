using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nienow.DataBaseConn;
using Nienow.Shinrikai.User;

namespace Nienow.Shinrikai.Api.Auth
{
    /// <summary>
    /// Ao iniciar a API, troca por hash PBKDF2 as senhas que ainda estão em texto puro (herdadas da base
    /// original) e aumenta USR_PASS se preciso. Roda em segundo plano para não atrasar a subida; enquanto
    /// isso, o login continua aceitando a senha antiga e já a converte. Se o banco ainda não respondeu
    /// (a VM do Firebird pode subir depois do container), tenta de novo algumas vezes.
    /// </summary>
    public sealed class PasswordMigration(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<PasswordMigration> logger)
        : BackgroundService
    {
        private const int Attempts = 5;
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(30);

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!ShinrikaiDB.IsConfigured(configuration.GetConnectionString("ShinrikaiDB")))
                return;

            for (var attempt = 1; attempt <= Attempts; attempt++)
            {
                try
                {
                    await using var scope = scopes.CreateAsyncScope();
                    var count = await scope.ServiceProvider.GetRequiredService<UserService>().HashPlainPasswordsAsync(stoppingToken);
                    if (count > 0)
                        logger.LogInformation("Senhas em texto puro convertidas para hash: {Count}.", count);
                    return;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Migração das senhas para hash falhou (tentativa {Attempt} de {Attempts}).", attempt, Attempts);
                    if (attempt == Attempts)
                        return;
                    await Task.Delay(RetryDelay, stoppingToken);
                }
            }
        }
    }
}
