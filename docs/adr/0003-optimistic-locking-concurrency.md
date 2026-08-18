---
status: proposed
---

# Lock otimista (RowVersion) para concorrência no Saldo

Requisito opcional: duas notas debitando o mesmo Produto simultaneamente não podem levar Saldo a negativo. Optou-se por lock otimista (coluna de versão no Produto via EF Core) em vez de lock pessimista (trava de linha no banco): mais simples de implementar e de demonstrar (duas tentativas concorrentes, uma falha com versão desatualizada e recebe erro claro). É o requisito opcional de menor prioridade — pode ser cortado se o prazo de 7 dias apertar.
