using System;

namespace Nienow.Helper
{
    /// <summary>
    /// Contador de acertos/erros de um item monitorado (palavra, kanji, exercício).
    /// Portado 1:1 de Nienow.Helper/Monitor.cs. <c>ID == -1</c> indica registro ainda não persistido.
    /// </summary>
    public class Monitor
    {
        public int ID { get; set; } = -1;
        public int UsrID { get; set; } = -1;
        /// <summary>ID do item monitorado (palavra, kanji ou exercício, conforme a tabela).</summary>
        public int VcbID { get; set; } = -1;
        public int FailCounter { get; set; }
        public int Counter { get; set; }
        public int SuccessCounter { get; set; }
        public DateTime LastHittedTime { get; set; } = DateTime.MinValue;

        public bool IsNew => ID == -1;
    }
}
