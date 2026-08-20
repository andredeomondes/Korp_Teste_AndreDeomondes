using Faturamento.Estoque;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Faturamento.NotasFiscais;

public sealed record AdicionarItemRequest(Guid ProdutoId, int Quantidade);

public sealed record AlterarQuantidadeRequest(int Quantidade);

public sealed record ItemDaNotaResponse(
    Guid Id,
    Guid ProdutoId,
    string Codigo,
    string Descricao,
    int Quantidade)
{
    public static ItemDaNotaResponse De(ItemDaNota item) =>
        new(item.Id, item.ProdutoId, item.Codigo, item.Descricao, item.Quantidade);
}

public static class ItensDaNotaEndpoints
{
    public static IEndpointRouteBuilder MapItensDaNota(this IEndpointRouteBuilder app)
    {
        var itens = app.MapGroup("/notas-fiscais/{notaId:guid}/itens").WithTags("Itens da Nota");

        itens.MapPost("/", async (
            Guid notaId,
            AdicionarItemRequest request,
            FaturamentoDbContext db,
            IEstoqueClient estoque,
            CancellationToken cancellationToken) =>
        {
            if (request.Quantidade <= 0)
            {
                return QuantidadeInvalida();
            }

            var nota = await db.NotasFiscais
                .Include(n => n.Itens)
                .FirstOrDefaultAsync(n => n.Id == notaId, cancellationToken);

            if (nota is null)
            {
                return Results.NotFound();
            }

            if (nota.Status != StatusNotaFiscal.Aberta)
            {
                return NotaFechada(nota);
            }

            // O Produto pertence ao Estoque: é lá que se descobre se ele existe e
            // qual a sua Descrição atual.
            ProdutoDoEstoque? produto;

            try
            {
                produto = await estoque.ObterProdutoAsync(request.ProdutoId, cancellationToken);
            }
            catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
            {
                return ProdutosDisponiveisEndpoints.EstoqueIndisponivel();
            }

            if (produto is null)
            {
                // 422 e não 404: a Nota Fiscal existe e a rota também — o que não
                // se sustenta é o Produto informado no corpo da requisição.
                return Erros.Recusa(
                    Erros.ProdutoInexistente,
                    "Produto inexistente",
                    "O Produto informado não existe no Estoque.",
                    StatusCodes.Status422UnprocessableEntity);
            }

            if (nota.Itens.Any(item => item.ProdutoId == produto.Id))
            {
                return ProdutoRepetido(produto.Codigo);
            }

            var novo = ItemDaNota.Novo(
                nota.Id,
                produto.Id,
                produto.Codigo,
                produto.Descricao,
                request.Quantidade);

            // Inserção explícita: a chave do Item já vem preenchida da aplicação,
            // e só juntá-lo à coleção faria o EF Core tomá-lo por linha existente.
            db.ItensDaNota.Add(novo);

            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException erro) when (ViolouUnicidadeDoProduto(erro))
            {
                // A checagem acima resolve o caso comum; o índice único resolve o
                // caso concorrente, em que duas requisições passaram pela checagem
                // antes de qualquer uma gravar.
                return ProdutoRepetido(produto.Codigo);
            }

            return Results.Created(
                $"/notas-fiscais/{nota.Id}/itens/{novo.Id}",
                ItemDaNotaResponse.De(novo));
        })
            .WithName("AdicionarItemDaNota");

        itens.MapPut("/{itemId:guid}", async (
            Guid notaId,
            Guid itemId,
            AlterarQuantidadeRequest request,
            FaturamentoDbContext db,
            CancellationToken cancellationToken) =>
        {
            if (request.Quantidade <= 0)
            {
                return QuantidadeInvalida();
            }

            var (nota, item) = await BuscarNotaEItemAsync(db, notaId, itemId, cancellationToken);

            if (nota is null || item is null)
            {
                return Results.NotFound();
            }

            if (nota.Status != StatusNotaFiscal.Aberta)
            {
                return NotaFechada(nota);
            }

            item.AlterarQuantidade(request.Quantidade);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(ItemDaNotaResponse.De(item));
        })
            .WithName("AlterarQuantidadeDoItemDaNota");

        itens.MapDelete("/{itemId:guid}", async (
            Guid notaId,
            Guid itemId,
            FaturamentoDbContext db,
            CancellationToken cancellationToken) =>
        {
            var (nota, item) = await BuscarNotaEItemAsync(db, notaId, itemId, cancellationToken);

            if (nota is null || item is null)
            {
                return Results.NotFound();
            }

            if (nota.Status != StatusNotaFiscal.Aberta)
            {
                return NotaFechada(nota);
            }

            db.ItensDaNota.Remove(item);
            await db.SaveChangesAsync(cancellationToken);

            return Results.NoContent();
        })
            .WithName("RemoverItemDaNota");

        return app;
    }

    /// <summary>
    /// Alterar ou remover um Item exige saber se a Nota Fiscal ainda está
    /// Aberta, e o Item sozinho não conta isso. Duas consultas diretas em vez de
    /// um include filtrado: o resultado não depende do que já esteja rastreado
    /// no contexto.
    /// </summary>
    private static async Task<(NotaFiscal? Nota, ItemDaNota? Item)> BuscarNotaEItemAsync(
        FaturamentoDbContext db,
        Guid notaId,
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var nota = await db.NotasFiscais.FirstOrDefaultAsync(
            nota => nota.Id == notaId,
            cancellationToken);

        if (nota is null)
        {
            return (null, null);
        }

        var item = await db.ItensDaNota.FirstOrDefaultAsync(
            item => item.Id == itemId && item.NotaFiscalId == notaId,
            cancellationToken);

        return (nota, item);
    }

    /// <summary>
    /// Depois de Fechada a Nota Fiscal é imutável: ela afirma um Saldo já
    /// debitado, e mexer nos Itens tornaria essa afirmação falsa.
    /// </summary>
    private static IResult NotaFechada(NotaFiscal nota) =>
        Erros.Recusa(
            Erros.NotaFechada,
            "Nota Fiscal fechada",
            $"A Nota Fiscal {nota.Numero} está Fechada e não pode mais ser alterada.");

    private static bool ViolouUnicidadeDoProduto(DbUpdateException erro) =>
        erro.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static IResult ProdutoRepetido(string codigo) =>
        Erros.Recusa(
            Erros.ProdutoRepetido,
            "Produto repetido",
            $"O Produto {codigo} já está na Nota Fiscal. Altere a quantidade do Item da Nota.");

    private static IResult QuantidadeInvalida() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["Quantidade"] = ["Quantidade do Item da Nota deve ser positiva."],
        });
}
