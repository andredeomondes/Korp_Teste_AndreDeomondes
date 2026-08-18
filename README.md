# Sistema de Emissão de Notas Fiscais

Cadastro de Produtos, emissão de Notas Fiscais e baixa de estoque na impressão, distribuído em dois microsserviços.

## Arquitetura

| Componente | Stack | Porta | Responsabilidade |
| --- | --- | --- | --- |
| `frontend` | Angular 22 + Angular Material | 4300 | Telas de Produtos e Notas Fiscais |
| `services/estoque` | .NET 10 + PostgreSQL | 5001 | Produtos e Saldo |
| `services/faturamento` | .NET 10 + PostgreSQL | 5002 | Notas Fiscais e Itens da Nota |
| `postgres` | PostgreSQL 17 | 5432 | Um banco por serviço (`estoque`, `faturamento`) |
| `rabbitmq` | RabbitMQ 4 | 5672 / 15672 | Comando de baixa de Saldo |

Cada serviço é dono do seu próprio banco. O Faturamento nunca lê tabelas do Estoque por SQL — apenas pela API REST e pela mensageria. As decisões de arquitetura estão registradas em [`docs/adr/`](./docs/adr), e o vocabulário do domínio em [`CONTEXT.md`](./CONTEXT.md).

## Como executar

Requisitos: Docker.

```bash
docker compose up --build
```

Depois de subir:

- Aplicação: <http://localhost:4300>
- API do Estoque: <http://localhost:5001/health>
- API do Faturamento: <http://localhost:5002/health>
- Painel do RabbitMQ: <http://localhost:15672> (`guest` / `guest`)

Para derrubar tudo, incluindo os dados:

```bash
docker compose down -v
```

## Desenvolvimento local

Requisitos: .NET SDK 10, Node 20+, Docker (para o banco e o broker).

```bash
# apenas a infraestrutura
docker compose up postgres rabbitmq -d

# em terminais separados
dotnet run --project services/estoque/Estoque.csproj
dotnet run --project services/faturamento/Faturamento.csproj
cd frontend && npm install && npm start
```

O frontend de desenvolvimento sobe em <http://localhost:4200> e consome os serviços em 5001 e 5002.

## Verificação de saúde

O endpoint `/health` de cada serviço testa a conexão com o próprio banco e responde `503` quando não o alcança. A tela inicial da aplicação consulta os dois e exibe o estado de cada um, tornando visível uma indisponibilidade sem precisar abrir logs.

## Detalhamento técnico

As respostas aos itens exigidos pela especificação do teste estão em [`docs/detalhamento-tecnico.md`](./docs/detalhamento-tecnico.md).
