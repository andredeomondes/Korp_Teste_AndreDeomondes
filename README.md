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

## Cenário de falha: o Estoque fora do ar

Requisito obrigatório do teste, reproduzível com a stack no ar.

1. Prepare uma Nota Fiscal Aberta com pelo menos um Item da Nota, em <http://localhost:4300/notas-fiscais>.
2. Derrube o Estoque e deixe o resto de pé:

   ```bash
   docker compose stop estoque
   ```

3. Clique em **Imprimir**. A tela mostra _Serviço indisponível_, com o botão **Tentar novamente** — e a Nota Fiscal **continua Aberta**, com seus itens intactos. Em **Status dos serviços**, o Estoque aparece como Indisponível. (Ao recarregar a tela com o Estoque parado, a lista de Produtos disponíveis também acusa a indisponibilidade — ela vem do Estoque.)
4. Suba o Estoque de volta e clique em **Tentar novamente**:

   ```bash
   docker compose start estoque
   ```

   A Impressão se completa, a nota vira Fechada e o Saldo é debitado uma única vez.

A tela separa dois casos que o protocolo aproxima: **indisponibilidade** (503) convida a repetir a operação; **recusa de negócio** (4xx, como Saldo insuficiente) diz o que houve e não oferece repetição, porque repetir sem mudar nada daria no mesmo.

## Testes

```bash
dotnet test                      # integração de Estoque e Faturamento (PostgreSQL real via Testcontainers)
cd frontend && npm test          # componentes Angular
```

Os testes de backend exigem Docker em execução: cada suíte sobe seu próprio contêiner PostgreSQL e aplica as migrations, sem depender do banco do `docker compose`.

## Verificação de saúde

O endpoint `/health` de cada serviço testa a conexão com o próprio banco e responde `503` quando não o alcança. A tela **Status dos serviços** consulta os dois e exibe o estado de cada um, tornando visível uma indisponibilidade sem precisar abrir logs.

## Detalhamento técnico

As respostas aos itens exigidos pela especificação do teste estão em [`docs/detalhamento-tecnico.md`](./docs/detalhamento-tecnico.md).
