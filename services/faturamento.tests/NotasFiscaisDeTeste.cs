using System.Net.Http.Json;

namespace Faturamento.Tests;

public sealed record NotaFiscalResponse(
    Guid Id,
    long Numero,
    string Status,
    ItemDaNotaResponse[] Itens);

public sealed record ItemDaNotaResponse(
    Guid Id,
    Guid ProdutoId,
    string Codigo,
    string Descricao,
    int Quantidade);

/// <summary>
/// Monta Notas Fiscais pela porta HTTP, para os testes falarem sobre o cenário
/// em vez de sobre requisições. Uma forma só destes helpers em todas as suítes
/// mantém os contratos alinhados.
/// </summary>
public static class NotasFiscaisDeTeste
{
    public static async Task<NotaFiscalResponse> CriarNotaAsync(
        this HttpClient client,
        params (Guid ProdutoId, int Quantidade)[] itens)
    {
        var criada = await client.PostAsync("/notas-fiscais", null);
        criada.EnsureSuccessStatusCode();

        var nota = (await criada.Content.ReadFromJsonAsync<NotaFiscalResponse>())!;

        foreach (var (produtoId, quantidade) in itens)
        {
            var resposta = await client.PostAsJsonAsync(
                $"/notas-fiscais/{nota.Id}/itens",
                new { produtoId, quantidade });

            resposta.EnsureSuccessStatusCode();
        }

        return nota;
    }

    public static async Task<NotaFiscalResponse> ObterNotaAsync(this HttpClient client, Guid id) =>
        (await client.GetFromJsonAsync<NotaFiscalResponse>($"/notas-fiscais/{id}"))!;
}
