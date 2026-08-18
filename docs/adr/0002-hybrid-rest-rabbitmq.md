# REST síncrono para leitura, RabbitMQ para baixa de estoque

Faturamento precisa consultar e debitar Saldo no Estoque. Consultas (listar produtos, saldo) usam REST síncrono, pois a UI precisa do dado na hora. A Impressão de Nota Fiscal (que debita Saldo) usa um comando assíncrono via RabbitMQ, escolhido em vez de Kafka por ser mais simples e adequado a mensagens de comando pontuais (não streaming de eventos em escala). Essa escolha também é o mecanismo usado para satisfazer o requisito de tratamento de falha: se a fila/o Estoque estiver indisponível, a impressão falha com feedback claro e a nota permanece Aberta.

**Sequenciamento**: o débito nasce como chamada REST síncrona e só depois é trocado por RabbitMQ, sobre comportamento já coberto por testes. Isso tira a fila do caminho crítico do prazo — todos os requisitos obrigatórios fecham antes de o broker entrar em cena. Se a troca não couber nos 7 dias, o sistema entrega válido com o débito síncrono e este ADR deve ser corrigido para refletir o que o código faz.

**Considered Options**: manter tudo síncrono via REST (mais simples, mas não demonstra desacoplamento nem dá tanto material técnico pra explicar); Kafka (overkill para o volume e a natureza de comando único deste caso).
