import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API } from './api-config';

/** Item cadastrado com Código, Descrição e Saldo. */
export interface Produto {
  id: string;
  codigo: string;
  descricao: string;
  saldo: number;
}

export type CadastrarProduto = Omit<Produto, 'id'>;

@Injectable({ providedIn: 'root' })
export class ProdutoService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API.estoque}/produtos`;

  listar(): Observable<Produto[]> {
    return this.http.get<Produto[]>(this.url);
  }

  cadastrar(produto: CadastrarProduto): Observable<Produto> {
    return this.http.post<Produto>(this.url, produto);
  }
}
