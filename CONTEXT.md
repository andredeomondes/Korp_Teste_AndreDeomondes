# Sistema de Emissão de Notas Fiscais

Sistema de estoque e faturamento para cadastro de produtos e emissão de notas fiscais, dividido em dois serviços: Estoque (produtos e saldo) e Faturamento (notas fiscais).

## Language

**Produto**:
Item cadastrado com Código, Descrição e Saldo, disponível para uso em Notas Fiscais. Pertence ao contexto Estoque.
_Avoid_: Item, mercadoria

**Saldo**:
Quantidade disponível em estoque de um Produto. Nunca fica negativo — uma operação que exigiria saldo negativo é rejeitada, não permitida parcialmente.
_Avoid_: Estoque (quando se refere à quantidade — Estoque é o nome do serviço/contexto, não da quantidade), Quantidade disponível

**Nota Fiscal**:
Documento com Numeração sequencial, Status e um ou mais Itens da Nota, emitido pelo contexto Faturamento. Depois de Fechada é imutável — não pode ter itens ou quantidades alterados.
_Avoid_: Fatura, Pedido, Nota

**Item da Nota**:
Associação entre uma Nota Fiscal e um Produto, com a quantidade utilizada daquele Produto na nota.
_Avoid_: Linha, Item do pedido

**Status (Nota Fiscal)**:
Estado da Nota Fiscal: **Aberta** (editável, ainda não impressa) ou **Fechada** (impressa, imutável, saldo já debitado). Não existe estado de cancelamento — fora de escopo.
_Avoid_: Ativa, Concluída, Cancelada

**Numeração sequencial**:
Identificador da Nota Fiscal, contador global único e crescente para todo o sistema, sem reuso de números mesmo se uma nota for removida.
_Avoid_: ID, Código da nota

**Impressão**:
Ação que fecha uma Nota Fiscal: só permitida quando Status é Aberta; debita o Saldo dos Produtos envolvidos e muda o Status para Fechada. O débito é atômico entre todos os Itens da Nota — ou todos são debitados, ou nenhum. Se falhar (ex.: Saldo insuficiente em qualquer Item da Nota, ou serviço de Estoque indisponível), nenhum Saldo é alterado e a nota permanece Aberta.
_Avoid_: Emissão, Finalização

## Contexts

- **Estoque**: dono de Produto e Saldo.
- **Faturamento**: dono de Nota Fiscal e Item da Nota; consulta e debita Saldo no Estoque via Impressão.
