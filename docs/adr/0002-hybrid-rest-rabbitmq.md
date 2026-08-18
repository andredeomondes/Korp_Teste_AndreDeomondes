# REST síncrono para leitura, RabbitMQ para baixa de Saldo

Faturamento precisa consultar e debitar Saldo no Estoque. Consultas (listar Produtos, ver Saldo) usam REST síncrono, pois a interface precisa apresentar essas informações imediatamente. A Impressão de uma Nota Fiscal — que debita Saldo — trafega por RabbitMQ como comando assíncrono, escolhido em vez de Kafka por oferecer filas duráveis, confirmação de publicação e novas tentativas, sendo adequado ao envio de comandos pontuais; retenção, replay e processamento de fluxos em grande escala não são requisitos deste sistema.

## Fluxo da Impressão

O Faturamento publica no RabbitMQ um comando solicitando a baixa do Saldo dos Produtos presentes nos Itens da Nota. Enquanto o comando é processado, a Nota Fiscal permanece Aberta, a interface exibe indicador de processamento e o botão de Impressão fica desabilitado. O Estoque publica o resultado da operação num canal de resposta; ao receber confirmação de sucesso, o Faturamento muda o Status para Fechada.

A baixa é atômica entre todos os Itens da Nota — ou todos os Saldos são debitados, ou nenhum — e idempotente, usando um identificador de operação estável derivado da Nota Fiscal para impedir débitos duplicados.

A interface descobre a conclusão por **polling** do estado da Nota Fiscal. SSE e WebSocket foram descartados: resolvem o mesmo problema a um custo de implementação maior, sem ganho perceptível na escala deste sistema.

## Comportamento diante de falhas

- **RabbitMQ indisponível na publicação**: a Impressão não é iniciada, a Nota Fiscal permanece Aberta e o operador recebe mensagem de erro.
- **Comando aceito, mas Estoque indisponível**: a mensagem permanece na fila e é processada automaticamente quando o Estoque volta. A interface segue informando que a Impressão está em processamento.
- **Baixa rejeitada** (ex.: Saldo insuficiente): nenhum Saldo é alterado, a Nota Fiscal permanece Aberta e o operador recebe o motivo da falha.

Note que isso **muda** o comportamento em relação à fase REST, onde o Estoque indisponível faz a Impressão falhar imediatamente e exige nova tentativa manual do operador. A recuperação automática pela fila é a resposta mais forte ao requisito obrigatório de recuperação de falha.

Dead-letter queue fica documentada como evolução, não implementada — o ganho neste escopo é retórico e o custo não é.

## Sequenciamento

O débito nasce como chamada REST síncrona e coberto por testes; a troca por RabbitMQ vem depois, sobre comportamento já testado. Isso tira a fila do caminho crítico do prazo: todos os requisitos obrigatórios fecham antes de o broker entrar em cena. Nenhum requisito do enunciado exige mensageria — dois microsserviços, tratamento de falha e banco real são satisfeitos pela versão REST. A fila é escolha deliberada por desacoplamento e recuperação automática. Se a troca não couber nos 7 dias, o sistema entrega válido com o débito síncrono e este ADR deve ser reescrito para refletir exclusivamente a arquitetura realmente entregue.

## Considered Options

- **REST síncrono para tudo**: menor complexidade e retorno imediato, mas cria dependência temporal entre Faturamento e Estoque — o Estoque fora significa Impressão impossível, sem recuperação automática.
- **Kafka**: complexidade desnecessária para este cenário (partições, offsets, retenção) sem requisito que a justifique.
