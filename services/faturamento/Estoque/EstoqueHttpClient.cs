using System.Net;

namespace Faturamento.Estoque;

/// <summary>
/// Produto como o Faturamento o enxerga: pertence ao Estoque. O Saldo vem junto
/// para que a tela mostre ao operador quanto existe antes de ele escolher a
/// quantidade — e o quanto sobrou depois da Impressão.
/// </summary>
public sealed record ProdutoDoEstoque(Guid Id, string Codigo, string Descricao, int Saldo);

public interface IEstoqueClient
{
    /// <summary>
    /// Busca um Produto no Estoque. Devolve <c>null</c> quando o Produto não
    /// existe lá. Lança <see cref="HttpRequestException"/> ou
    /// <see cref="TaskCanceledException"/> quando o Estoque não pôde ser
    /// alcançado — cabe ao chamador traduzir isso para o operador.
    /// </summary>
    Task<ProdutoDoEstoque?> ObterProdutoAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Lista os Produtos disponíveis para entrar numa Nota Fiscal.</summary>
    Task<IReadOnlyList<ProdutoDoEstoque>> ListarProdutosAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Pede ao Estoque a baixa de Saldo de todos os itens de uma vez. O Estoque
    /// trata o pedido como uma operação só: ou debita tudo, ou não debita nada.
    /// </summary>
    Task<ResultadoDoDebito> DebitarSaldoAsync(
        IReadOnlyList<ItemDoDebito> itens,
        CancellationToken cancellationToken);
}

public sealed record ItemDoDebito(Guid ProdutoId, int Quantidade);

/// <summary>
/// O que o Estoque respondeu ao pedido de débito. A recusa vem com motivo
/// legível, porque é ele que a tela mostra ao operador.
/// </summary>
public sealed record ResultadoDoDebito(bool Debitou, string? Motivo)
{
    public static readonly ResultadoDoDebito Sucesso = new(true, null);

    public static ResultadoDoDebito Recusado(string motivo) => new(false, motivo);
}

public sealed class EstoqueHttpClient(HttpClient http) : IEstoqueClient
{
    public async Task<ProdutoDoEstoque?> ObterProdutoAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var resposta = await http.GetAsync($"/produtos/{id}", cancellationToken);

        if (resposta.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        resposta.EnsureSuccessStatusCode();

        return await resposta.Content.ReadFromJsonAsync<ProdutoDoEstoque>(cancellationToken);
    }

    public async Task<IReadOnlyList<ProdutoDoEstoque>> ListarProdutosAsync(
        CancellationToken cancellationToken)
    {
        var produtos = await http.GetFromJsonAsync<List<ProdutoDoEstoque>>(
            "/produtos",
            cancellationToken);

        return produtos ?? [];
    }

    public async Task<ResultadoDoDebito> DebitarSaldoAsync(
        IReadOnlyList<ItemDoDebito> itens,
        CancellationToken cancellationToken)
    {
        var resposta = await http.PostAsJsonAsync(
            "/debitos",
            new { itens },
            cancellationToken);

        if (resposta.IsSuccessStatusCode)
        {
            return ResultadoDoDebito.Sucesso;
        }

        // O Estoque recusou por uma razão do domínio (Saldo insuficiente, por
        // exemplo). O motivo vem em ProblemDetails e é repassado ao operador.
        var problema = await resposta.Content
            .ReadFromJsonAsync<ProblemDetails>(cancellationToken);

        return ResultadoDoDebito.Recusado(
            problema?.Detail ?? "O Estoque recusou a baixa de Saldo.");
    }

    private sealed record ProblemDetails(string? Title, string? Detail);
}
