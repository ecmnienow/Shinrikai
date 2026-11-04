namespace Nienow.Helper;

/// <summary>
/// Contador de acertos/erros de um item monitorado (palavra, kanji, exercício).
/// Portado 1:1 de Nienow.Helper/Monitor.cs. <c>ID == -1</c> indica registro ainda não persistido.
/// </summary>
public class Monitor
{
    public Monitor()
    {
        ID = -1;
        UsrID = -1;
        VcbID = -1;
        SuccessCounter = 0;
        FailCounter = 0;
        Counter = 0;
        LastHittedTime = DateTime.MinValue;
    }

    public int ID { get; set; }
    public int UsrID { get; set; }
    /// <summary>ID do item monitorado (palavra, kanji ou exercício, conforme a tabela).</summary>
    public int VcbID { get; set; }
    public int FailCounter { get; set; }
    public int Counter { get; set; }
    public int SuccessCounter { get; set; }
    public DateTime LastHittedTime { get; set; }

    public bool IsNew => ID == -1;
}
