# EF Core no acesso a dados e PostgreSQL real nos testes

O Estoque nasceu com `NpgsqlDataSource` cru, suficiente para o healthcheck. Com o Cadastro de Produto entra persistência de verdade, e o acesso a dados passa a ser feito por **EF Core** (`Npgsql.EntityFrameworkCore.PostgreSQL`): traz migrations versionadas no repositório, mapeamento do schema junto da entidade e o suporte a lock otimista por coluna de versão que o [ADR 0003](./0003-optimistic-locking-concurrency.md) já pressupõe. As migrations são aplicadas na subida do serviço, para que nenhum passo manual fique entre `docker compose up` e a API operante — e uma falha ao migrar registra log crítico sem derrubar o processo, senão `/health` não teria como reportar o banco inalcançável.

As restrições que sustentam o domínio ficam no schema, não apenas no código: índice único no Código e `CHECK (saldo >= 0)`. O Código é normalizado para maiúsculas antes de gravar, para que `caf-001` e `CAF-001` colidam no índice em vez de virarem dois Produtos.

Os testes do Estoque rodam contra um **PostgreSQL real** subido por Testcontainers, na seam HTTP: a aplicação inteira sobe via `WebApplicationFactory` e os testes falam com ela por HTTP. Um banco em memória (SQLite/InMemory) não executaria o índice único nem o `CHECK`, que são justamente as regras sob teste — passaria verde enquanto o banco de produção recusaria a gravação.

## Considered Options

- **Dapper ou Npgsql cru**: SQL explícito e menos camada, mas exigiria escrever e versionar migrations à mão e implementar o controle de concorrência sem apoio do ORM.
- **Banco em memória nos testes**: mais rápido e sem dependência de Docker, mas não executa restrições do PostgreSQL — testaria um banco que não é o de produção.
