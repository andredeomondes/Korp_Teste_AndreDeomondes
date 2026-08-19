namespace Faturamento.NotasFiscais;

/// <summary>
/// Associação entre uma Nota Fiscal e um Produto, com a quantidade utilizada
/// daquele Produto na nota.
/// </summary>
public sealed class ItemDaNota
{
    private ItemDaNota()
    {
    }

    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid NotaFiscalId { get; init; }

    /// <summary>Identidade do Produto no Estoque — o dono dele é o outro serviço.</summary>
    public Guid ProdutoId { get; init; }

    /// <summary>
    /// Código e Descrição são copiados do Estoque no momento em que o Item entra
    /// na nota. A Nota Fiscal precisa continuar legível mesmo que o Produto mude
    /// de descrição depois — e a listagem não pode depender de o Estoque estar no
    /// ar para se desenhar.
    /// </summary>
    public string Codigo { get; init; } = string.Empty;

    public string Descricao { get; init; } = string.Empty;

    public int Quantidade { get; private set; }

    public static ItemDaNota Novo(
        Guid notaFiscalId,
        Guid produtoId,
        string codigo,
        string descricao,
        int quantidade) =>
        new()
        {
            NotaFiscalId = notaFiscalId,
            ProdutoId = produtoId,
            Codigo = codigo,
            Descricao = descricao,
            Quantidade = Positiva(quantidade),
        };

    public void AlterarQuantidade(int quantidade) => Quantidade = Positiva(quantidade);

    /// <summary>
    /// Quantidade zero ou negativa não descreve Item da Nota nenhum: um Item
    /// assim precisa ser removido, não zerado. O schema repete a regra, para que
    /// nem uma gravação fora da aplicação consiga criá-lo.
    /// </summary>
    private static int Positiva(int quantidade) => quantidade > 0
        ? quantidade
        : throw new ArgumentOutOfRangeException(
            nameof(quantidade),
            "Quantidade do Item da Nota deve ser positiva.");
}
