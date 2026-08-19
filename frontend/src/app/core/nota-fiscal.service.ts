import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { API } from './api-config';

export type StatusNotaFiscal = 'Aberta' | 'Fechada';

/** Documento com Numeração sequencial, Status e Itens da Nota. */
export interface NotaFiscal {
  id: string;
  numero: number;
  status: StatusNotaFiscal;
}

@Injectable({ providedIn: 'root' })
export class NotaFiscalService {
  private readonly http = inject(HttpClient);
  private readonly url = `${API.faturamento}/notas-fiscais`;

  listar(): Observable<NotaFiscal[]> {
    return this.http.get<NotaFiscal[]>(this.url);
  }

  /**
   * A Nota Fiscal nasce sem dado nenhum do operador: a Numeração vem do
   * servidor e o Status nasce Aberta.
   */
  criar(): Observable<NotaFiscal> {
    return this.http.post<NotaFiscal>(this.url, null);
  }
}
