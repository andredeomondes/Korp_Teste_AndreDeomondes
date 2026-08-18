namespace Estoque.Produtos;

/// <summary>
/// Item cadastrado com Código, Descrição e Saldo, disponível para uso em Notas
/// Fiscais.
/// </summary>
public sealed class Produto
{
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required string Codigo { get; init; }

    public required string Descricao { get; set; }

    /// <summary>
    /// Quantidade disponível em estoque. Nunca fica negativo — o schema recusa
    /// qualquer gravação que tentaria levá-lo abaixo de zero.
    /// </summary>
    public required int Saldo { get; set; }
}
