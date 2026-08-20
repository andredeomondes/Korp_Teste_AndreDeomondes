using Faturamento.Estoque;
using Microsoft.EntityFrameworkCore;

namespace Faturamento.NotasFiscais;

public static class ImpressaoEndpoints
{
    /// <summary>
    /// Impressão: fecha a Nota Fiscal e debita o Saldo dos Produtos usados.
    /// Nesta fase o débito é uma chamada REST síncrona ao Estoque — o ADR 0002
    /// registra a troca por RabbitMQ como passo seguinte, sobre este mesmo
    /// comportamento já testado.
    /// </summary>
    public static IEndpointRouteBuilder MapImpressao(this IEndpointRouteBuilder app)
    {
        app.MapPost("/notas-fiscais/{notaId:guid}/impressao", async (
            Guid notaId,
            FaturamentoDbContext db,
            IEstoqueClient estoque,
            CancellationToken cancellationToken) =>
        {
            var nota = await db.NotasFiscais
                .Include(n => n.Itens)
                .FirstOrDefaultAsync(n => n.Id == notaId, cancellationToken);

            if (nota is null)
            {
                return Results.NotFound();
            }

            // Impressão só existe para nota Aberta. Sem esta guarda, imprimir
            // duas vezes debitaria o Saldo duas vezes.
            if (nota.Status != StatusNotaFiscal.Aberta)
            {
                return Results.Problem(
                    title: "Nota Fiscal já impressa",
                    detail: $"A Nota Fiscal {nota.Numero} está Fechada e não pode ser impressa "
                        + "de novo.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            if (nota.Itens.Count == 0)
            {
                return Results.Problem(
                    title: "Nota Fiscal sem itens",
                    detail: "Adicione ao menos um Item da Nota antes de imprimir.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            var itens = nota.Itens
                .Select(item => new ItemDoDebito(item.ProdutoId, item.Quantidade))
                .ToList();

            ResultadoDoDebito resultado;

            try
            {
                resultado = await estoque.DebitarSaldoAsync(itens, cancellationToken);
            }
            catch (Exception erro) when (erro is HttpRequestException or TaskCanceledException)
            {
                // Sem confirmação do Estoque a nota continua Aberta: fechá-la
                // aqui afirmaria um débito que ninguém sabe se aconteceu.
                return ProdutosDisponiveisEndpoints.EstoqueIndisponivel();
            }

            if (!resultado.Debitou)
            {
                return Results.Problem(
                    title: "Impressão recusada",
                    detail: resultado.Motivo,
                    statusCode: StatusCodes.Status409Conflict);
            }

            // O Saldo já foi debitado: só agora a nota vira Fechada.
            nota.Fechar();
            await db.SaveChangesAsync(cancellationToken);

            return Results.Ok(NotaFiscalResponse.De(nota));
        })
            .WithName("ImprimirNotaFiscal")
            .WithTags("Impressão");

        return app;
    }
}
