namespace Faturamento.NotasFiscais;

/// <summary>
/// Documento com Numeração sequencial, Status e um ou mais Itens da Nota.
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

    public List<ItemDaNota> Itens { get; init; } = [];

    /// <summary>
    /// Fecha a Nota Fiscal. Só a Impressão chama isto, e só depois de o Estoque
    /// confirmar a baixa do Saldo — uma nota Fechada afirma que o débito
    /// aconteceu.
    /// </summary>
    public void Fechar() => Status = StatusNotaFiscal.Fechada;
}
