# Detalhamento técnico

Respostas aos itens exigidos pela especificação do teste. Um heading por item, na ordem do enunciado.

> **Status**: documento em construção — será preenchido conforme as funcionalidades forem implementadas. Ver issue #13.

## 1. Quais ciclos de vida do Angular foram utilizados

_A preencher._

## 2. Uso da biblioteca RxJS

O `HealthService` consome os endpoints `/health` dos dois microsserviços e usa `map`, `catchError` e `of` para converter uma falha de rede em **estado** (`offline`) em vez de propagar o erro. Isso permite que a tela exiba a indisponibilidade de um serviço sem interromper a verificação do outro.

_Demais usos a preencher conforme a implementação avança._

## 3. Quais outras bibliotecas foram utilizadas e para qual finalidade

| Biblioteca | Onde | Finalidade |
| --- | --- | --- |
| Npgsql | Estoque, Faturamento | Driver PostgreSQL para .NET |

_A completar._

## 4. Bibliotecas de componentes visuais

**Angular Material** (`@angular/material` 22), tema Material 3 com paleta azure.

A escolha inicial havia sido PrimeNG, revista após constatar que a versão 22 passou a ser closed-source e exibe um aviso de licença inválida em tempo de execução sem uma chave configurada. As versões 21 e anteriores permanecem MIT, mas exigiriam fixar o projeto numa versão anterior do Angular. Angular Material é MIT, mantido pelo próprio time do Angular e compatível com o Angular 22 já utilizado.

## 5. Gerenciamento de dependências no Golang

**Não se aplica.** O backend foi implementado em C#, alternativa permitida pela especificação. As dependências .NET são gerenciadas por NuGet, declaradas nos arquivos `.csproj` de cada serviço e restauradas com `dotnet restore`.

## 6. Frameworks utilizados no C#

_A preencher._

## 7. Tratamento de erros e exceções no backend

_A preencher._

## 8. Uso de LINQ

_A preencher._
