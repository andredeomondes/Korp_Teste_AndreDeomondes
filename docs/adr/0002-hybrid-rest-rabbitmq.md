# REST síncrono para leitura, RabbitMQ para baixa de estoque

Faturamento precisa consultar e debitar Saldo no Estoque. Consultas (listar produtos, saldo) usam REST síncrono, pois a UI precisa do dado na hora. A Impressão de Nota Fiscal (que debita Saldo) usa um comando assíncrono via RabbitMQ, escolhido em vez de Kafka por ser mais simples e adequado a mensagens de comando pontuais (não streaming de eventos em escala). Essa escolha também é o mecanismo usado para satisfazer o requisito de tratamento de falha: se a fila/o Estoque estiver indisponível, a impressão falha com feedback claro e a nota permanece Aberta.

**Considered Options**: manter tudo síncrono via REST (mais simples, mas não demonstra desacoplamento nem dá tanto material técnico pra explicar); Kafka (overkill para o volume e a natureza de comando único deste caso).
