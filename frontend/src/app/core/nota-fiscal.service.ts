import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API } from './api-config';

export type StatusNotaFiscal = 'Aberta' | 'Fechada';

/** Produto que pode ser incluído numa Nota Fiscal, como o Faturamento o expõe. */
export interface ProdutoDisponivel {
  id: string;
  codigo: string;
  descricao: string;
  saldo: number;
}

/** Associação entre uma Nota Fiscal e um Produto, com a quantidade utilizada. */
export interface ItemDaNota {
  id: string;
  produtoId: string;
  codigo: string;
  descricao: string;
  quantidade: number;
}

/** A Nota Fiscal como aparece na listagem: sem os Itens da Nota. */
export interface NotaFiscalResumo {
  id: string;
  numero: number;
  status: StatusNotaFiscal;
  quantidadeDeItens: number;
}

/** Documento com Numeração sequencial, Status e Itens da Nota. */
export interface NotaFiscal {
  id: string;
  numero: number;
  status: StatusNotaFiscal;
  itens: ItemDaNota[];
}

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API.faturamento}/notas-fiscais`;

  /**
   * Os Produtos que podem entrar numa Nota Fiscal. Quem responde é o
   * Faturamento, ainda que o dado seja do Estoque: assim esta tela conversa com
   * um serviço só e recebe a indisponibilidade do Estoque já traduzida.
   */
  produtosDisponiveis(): Observable<ProdutoDisponivel[]> {
    return this.http.get<ProdutoDisponivel[]>(`${API.faturamento}/produtos-disponiveis`);
  }

  listar(): Observable<NotaFiscalResumo[]> {
    return this.http.get<NotaFiscalResumo[]>(this.url);
  }

  obter(id: string): Observable<NotaFiscal> {
    return this.http.get<NotaFiscal>(`${this.url}/${id}`);
  }

  adicionarItem(notaId: string, produtoId: string, quantidade: number): Observable<ItemDaNota> {
    return this.http.post<ItemDaNota>(`${this.url}/${notaId}/itens`, { produtoId, quantidade });
  }

  alterarQuantidade(notaId: string, itemId: string, quantidade: number): Observable<ItemDaNota> {
    return this.http.put<ItemDaNota>(`${this.url}/${notaId}/itens/${itemId}`, { quantidade });
  }

  /**
   * Fecha a Nota Fiscal e debita o Saldo dos Produtos usados. A resposta traz a
   * nota já Fechada.
   */
  imprimir(notaId: string): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(`${this.url}/${notaId}/impressao`, null);
  }

  removerItem(notaId: string, itemId: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${notaId}/itens/${itemId}`);
  }

  /**
   * A Nota Fiscal nasce sem dado nenhum do operador: a Numeração vem do
   * servidor e o Status nasce Aberta.
   */
  criar(): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.url, null);
  }
}
