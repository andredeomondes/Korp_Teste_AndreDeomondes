namespace Faturamento.NotasFiscais;

/// <summary>
/// Documento com Numeração sequencial e Status. Os Itens da Nota ainda não
/// fazem parte do modelo.
/// </summary>
public sealed class NotaFiscal
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    /// <summary>
    /// Numeração sequencial: contador global e crescente, atribuído pelo
    /// PostgreSQL na inserção. Nunca é escrito pela aplicação.
    /// </summary>
    public long Numero { get; private set; }

    /// <summary>
    /// Toda Nota Fiscal nasce Aberta. A transição para Fechada pertence à
    /// Impressão, que ainda não existe — por isso não há como mudá-lo de fora.
    /// </summary>
    public StatusNotaFiscal Status { get; private set; } = StatusNotaFiscal.Aberta;
}
