namespace Faturamento.NotasFiscais;

/// <summary>
/// Estado da Nota Fiscal. Não existe cancelamento — está fora de escopo.
/// </summary>
public enum StatusNotaFiscal
{
    /// <summary>Editável, ainda não impressa.</summary>
    Aberta,

    /// <summary>Impressa, imutável, Saldo já debitado.</summary>
    Fechada,
}
