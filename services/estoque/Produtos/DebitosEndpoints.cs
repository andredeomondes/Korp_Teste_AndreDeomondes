using Microsoft.EntityFrameworkCore;

namespace Estoque.Produtos;

public sealed record ItemDoDebito(Guid ProdutoId, int Quantidade);

public sealed record DebitarSaldoRequest(IReadOnlyList<ItemDoDebito> Itens);

public static class DebitosEndpoints
{
    /// <summary>
    /// Baixa de Saldo pedida pela Impressão de uma Nota Fiscal. É atômica entre
    /// todos os itens — ou todos os Saldos são debitados, ou nenhum é — porque
    /// uma nota meio impressa deixaria o Estoque mentindo sobre o que existe.
    /// </summary>
    public static IEndpointRouteBuilder MapDebitos(this IEndpointRouteBuilder app)
    {
        app.MapPost("/debitos", async (
            DebitarSaldoRequest request,
            EstoqueDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (request.Itens.Count == 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Itens"] = ["Informe ao menos um item para debitar."],
                });
            }

            // Quantidade inválida é erro de quem chamou, não Saldo insuficiente:
            // sem esta checagem a recusa sairia com uma mensagem que mente sobre
            // o motivo.
            if (request.Itens.Any(item => item.Quantidade <= 0))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["Quantidade"] = ["A quantidade a debitar deve ser positiva."],
                });
            }

            await using var transacao = await db.Database.BeginTransactionAsync(cancellationToken);

            var ids = request.Itens.Select(item => item.ProdutoId).ToList();

            var produtos = await db.Produtos
                .Where(produto => ids.Contains(produto.Id))
                .ToDictionaryAsync(produto => produto.Id, cancellationToken);

            // Duas passagens de propósito: a primeira só decide, a segunda só
            // aplica. Assim nenhum Produto chega a ser alterado quando a operação
            // vai ser recusada — a atomicidade não fica dependendo apenas do
            // rollback da transação.
            foreach (var item in request.Itens)
            {
                if (!produtos.TryGetValue(item.ProdutoId, out var produto))
                {
                    return Erros.Recusa(
                        Erros.ProdutoInexistente,
                        "Produto inexistente",
                        $"O Produto {item.ProdutoId} não existe no Estoque.",
                        StatusCodes.Status422UnprocessableEntity);
                }

                if (item.Quantidade > produto.Saldo)
                {
                    return Erros.Recusa(
                        Erros.SaldoInsuficiente,
                        "Saldo insuficiente",
                        $"O Produto {produto.Codigo} tem Saldo {produto.Saldo} "
                            + $"e a operação pediu {item.Quantidade}.");
                }
            }

            foreach (var item in request.Itens)
            {
                produtos[item.ProdutoId].Debitar(item.Quantidade);
            }

            await db.SaveChangesAsync(cancellationToken);
            await transacao.CommitAsync(cancellationToken);

            return Results.NoContent();
        })
            .WithName("DebitarSaldo")
            .WithTags("Saldo");

        return app;
    }
}
