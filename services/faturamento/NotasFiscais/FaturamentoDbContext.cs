using Microsoft.EntityFrameworkCore;

namespace Faturamento.NotasFiscais;

public sealed class FaturamentoDbContext(DbContextOptions<FaturamentoDbContext> options)
    : DbContext(options)
{
    private const string SequenceNumeracao = "numeracao_nota_fiscal";

    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();

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
        });
    }
}
