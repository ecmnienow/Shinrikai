using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nienow.DataBaseConn;
using Monitor = Nienow.Helper.Monitor;

namespace Nienow.Shinrikai.Tests
{
    /// <summary>Tabelas de monitor em memória. Guarda cópias, como o banco (alterar o objeto lido não grava).</summary>
    public sealed class FakeMonitors : IMonitorStore
    {
        public Dictionary<(string Table, int User, int Item), Monitor> Rows { get; } = [];

        public Monitor Get(MonitorTable table, int userId, int itemId) => Rows[(table.Table, userId, itemId)];

        public Task<Monitor?> GetAsync(MonitorTable table, int userId, int itemId, CancellationToken ct = default) =>
            Task.FromResult(Rows.TryGetValue((table.Table, userId, itemId), out var m) ? Clone(m) : null);

        public Task<IReadOnlyDictionary<int, int>> GetCountersAsync(MonitorTable table, int userId, IReadOnlyList<int> itemIds, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, int>>(Rows
                .Where(r => r.Key.Table == table.Table && r.Key.User == userId && itemIds.Contains(r.Key.Item))
                .ToDictionary(r => r.Key.Item, r => r.Value.Counter));

        public Task SaveAsync(MonitorTable table, Monitor monitor, CancellationToken ct = default)
        {
            if (monitor.IsNew)
                monitor.ID = Rows.Count + 1;
            Rows[(table.Table, monitor.UsrID, monitor.VcbID)] = Clone(monitor);
            return Task.CompletedTask;
        }

        private static Monitor Clone(Monitor m) => new()
        {
            ID = m.ID, UsrID = m.UsrID, VcbID = m.VcbID, Counter = m.Counter,
            FailCounter = m.FailCounter, SuccessCounter = m.SuccessCounter, LastHittedTime = m.LastHittedTime,
        };
    }
}
