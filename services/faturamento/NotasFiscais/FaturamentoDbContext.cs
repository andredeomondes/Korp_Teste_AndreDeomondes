using Microsoft.EntityFrameworkCore;

namespace Faturamento.NotasFiscais;

public sealed class FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options)
    : DbContext(options)
{
    private const string SequenceNumeracao = "numeracao_nota_fiscal";

    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

    public DbSet<ItemDaNota> ItensDaNota => Set<ItemDaNota>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // A Numeração é um contador global do sistema, não da tabela: uma
        // sequence do PostgreSQL entrega números únicos e crescentes mesmo sob
        // inserções concorrentes, e não reaproveita números de notas removidas.
        modelBuilder.HasSequence<long>(SequenceNumeracao).StartsAt(1).IncrementsBy(1);

        modelBuilder.Entity<NotaFiscal>(nota =>
        {
            // Aberta e Fechada são os únicos estados que existem no domínio, e
            // quem garante isso é o schema: uma gravação fora da aplicação não
            // consegue inventar um terceiro.
            nota.ToTable("notas_fiscais", table => table.HasCheckConstraint(
                "ck_notas_fiscais_status",
                "status IN ('Aberta', 'Fechada')"));

            nota.HasKey(n => n.Id);
            nota.Property(n => n.Id).HasColumnName("id");

            nota.Property(n => n.Numero)
                .HasColumnName("numero")
                .HasDefaultValueSql($"nextval('{SequenceNumeracao}')")
                .ValueGeneratedOnAdd();

            nota.HasIndex(n => n.Numero).IsUnique();

            // O Status é gravado pelo nome ('Aberta'/'Fechada') e não pelo
            // número do enum: a coluna continua legível fora da aplicação e
            // sobrevive a uma reordenação do enum.
            nota.Property(n => n.Status)
                .HasColumnName("status")
                .HasMaxLength(20)
                .HasConversion<string>();

            nota.HasMany(n => n.Itens)
                .WithOne()
                .HasForeignKey(item => item.NotaFiscalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ItemDaNota>(item =>
        {
            item.ToTable("itens_da_nota", table => table.HasCheckConstraint(
                "ck_itens_da_nota_quantidade_positiva",
                "quantidade > 0"));

            item.HasKey(i => i.Id);

            item.Property(i => i.Id).HasColumnName("id");
            item.Property(i => i.NotaFiscalId).HasColumnName("nota_fiscal_id");
            item.Property(i => i.ProdutoId).HasColumnName("produto_id");
            item.Property(i => i.Codigo).HasColumnName("codigo").HasMaxLength(50);
            item.Property(i => i.Descricao).HasColumnName("descricao").HasMaxLength(200);
            item.Property(i => i.Quantidade).HasColumnName("quantidade");

            // Um Produto entra uma vez só em cada Nota Fiscal: repetir o mesmo
            // Produto em duas linhas tornaria ambíguo o total a debitar na
            // Impressão. Para levar mais unidades, altera-se a quantidade.
            item.HasIndex(i => new { i.NotaFiscalId, i.ProdutoId }).IsUnique();
        });
    }
}
