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

    /// <summary>
    /// Retira do Saldo a quantidade usada por um Item da Nota. Quem chama já
    /// decidiu que a operação cabe; o Produto apenas se recusa a entrar em
    /// estado inválido, e isso é falha de programação, não do operador.
    /// </summary>
    public void Debitar(int quantidade)
    {
        if (quantidade <= 0 || quantidade > Saldo)
        {
            throw new InvalidOperationException(
                $"Débito de {quantidade} inválido para o Produto {Codigo} com Saldo {Saldo}.");
        }

        Saldo -= quantidade;
    }
}
