using System.Net;
using System.Net.Http.Json;

namespace Faturamento.Tests;

/// <summary>
/// Faz o papel do serviço de Estoque nos testes do Faturamento. O Estoque é um
/// sistema externo, não um colaborador interno: substituí-lo aqui mantém os
/// testes na seam HTTP do Faturamento sem precisar subir o outro serviço.
/// </summary>
public sealed class EstoqueFalso : HttpMessageHandler
{
    private readonly Dictionary<Guid, object> _produtos = [];

    /// <summary>Quando ligado, toda chamada ao Estoque falha como se ele estivesse fora.</summary>
    public bool ForaDoAr { get; set; }

    public Guid Cadastrar(string codigo, string descricao, int saldo)
    {
        var id = Guid.CreateVersion7();
        _produtos[id] = new
        {
            id,
            codigo,
            descricao,
            saldo,
        };
        return id;
    }

    public void Limpar()
    {
        _produtos.Clear();
        ForaDoAr = false;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (ForaDoAr)
        {
            throw new HttpRequestException("Estoque fora do ar.");
        }

        var caminho = request.RequestUri?.AbsolutePath ?? string.Empty;

        // GET /produtos lista; GET /produtos/{id} busca um.
        if (caminho.TrimEnd('/').Equals("/produtos", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(_produtos.Values),
            });
        }

        var id = Guid.TryParse(caminho[(caminho.LastIndexOf('/') + 1)..], out var parsed)
            ? parsed
            : Guid.Empty;

        var resposta = _produtos.TryGetValue(id, out var produto)
            ? new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(produto) }
            : new HttpResponseMessage(HttpStatusCode.NotFound);

        return Task.FromResult(resposta);
    }
}
