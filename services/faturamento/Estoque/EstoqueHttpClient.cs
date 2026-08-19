using System.Net;

namespace Faturamento.Estoque;

/// <summary>Produto como o Faturamento o enxerga: pertence ao Estoque.</summary>
public sealed record ProdutoDoEstoque(Guid Id, string Codigo, string Descricao);

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
}
