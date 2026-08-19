using Microsoft.EntityFrameworkCore;

namespace Faturamento.NotasFiscais;

public sealed record NotaFiscalResponse(Guid Id, long Numero, string Status)
{
    public static NotaFiscalResponse De(NotaFiscal nota) =>
        new(nota.Id, nota.Numero, nota.Status.ToString());
}

public static class NotasFiscaisEndpoints
{
    public static IEndpointRouteBuilder MapNotasFiscais(this IEndpointRouteBuilder app)
    {
        var notas = app.MapGroup("/notas-fiscais").WithTags("Notas Fiscais");

        notas.MapGet("/", async (FaturamentoDbContext db, CancellationToken cancellationToken) =>
            // A projeção é traduzida para SQL, então precisa ser construída aqui
            // dentro: uma chamada a NotaFiscalResponse.De não sobreviveria à
            // tradução da árvore de expressão.
            await db.NotasFiscais
                .OrderBy(nota => nota.Numero)
                .Select(nota => new NotaFiscalResponse(nota.Id, nota.Numero, nota.Status.ToString()))
                .ToListAsync(cancellationToken))
            .WithName("ListarNotasFiscais");

        notas.MapGet("/{id:guid}", async (
            Guid id,
            FaturamentoDbContext db,
            CancellationToken cancellationToken) =>
        {
            var nota = await db.NotasFiscais.FindAsync([id], cancellationToken);

            return nota is null
                ? Results.NotFound()
                : Results.Ok(NotaFiscalResponse.De(nota));
        })
            .WithName("ObterNotaFiscal");

        notas.MapPost("/", async (FaturamentoDbContext db, CancellationToken cancellationToken) =>
        {
            // A Nota Fiscal nasce sem nenhum dado do operador: Numeração vem da
            // sequence e Status nasce Aberta. Os Itens da Nota entram depois.
            var nota = new NotaFiscal();

            db.NotasFiscais.Add(nota);
            await db.SaveChangesAsync(cancellationToken);

            return Results.Created($"/notas-fiscais/{nota.Id}", NotaFiscalResponse.De(nota));
        })
            .WithName("CriarNotaFiscal");

        return app;
    }
}
