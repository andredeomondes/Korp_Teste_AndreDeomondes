-- Cada microsserviço é dono do seu próprio banco: o Faturamento não alcança as
-- tabelas do Estoque por SQL, apenas pela API e pela mensageria.
CREATE DATABASE estoque;
CREATE DATABASE faturamento;
