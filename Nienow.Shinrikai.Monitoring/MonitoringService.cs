using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Nienow.DataBaseConn;

namespace Nienow.Shinrikai.Monitoring
{
    /// <summary>
    /// Log de interações do usuário (tabela INTERACTIONS) — port de MonitoringManager/DBMonitoring.
    /// É dado de pesquisa do TCC; falhas aqui são registradas mas não derrubam a requisição.
    /// </summary>
    public interface IMonitoringService
    {
        Task AddAsync(int userId, string screen, string action, string? option, string? sessionId, CancellationToken ct = default);
    }

    public sealed class MonitoringService(IShinrikaiDb db, ILogger<MonitoringService> logger) : IMonitoringService
    {
        public async Task AddAsync(int userId, string screen, string action, string? option, string? sessionId, CancellationToken ct = default)
        {
            try
            {
                await using var conn = await db.OpenAsync(ct);
                await conn.ExecuteInTransactionAsync(
                    """
                    INSERT INTO INTERACTIONS (
                      ITR_ID, ITR_USR_ID, ITR_SCR_NAME, ITR_SCR_ACTION, ITR_SCR_OPTION, ITR_DATETIME, ITR_SESSIONID
                    ) VALUES (
                      GEN_ID(INTERACTIONS_ID_GEN, 1), @USR, @NAM, @ACT, @OPT, @DAT, @SES
                    )
                    """,
                    ct,
                    ("@USR", userId),
                    ("@NAM", screen),
                    ("@ACT", action),
                    ("@OPT", option),
                    ("@DAT", DateTime.Now),
                    ("@SES", sessionId));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Falha ao registrar interação {Screen}/{Action} do usuário {UserId}", screen, action, userId);
            }
        }
    }
}
