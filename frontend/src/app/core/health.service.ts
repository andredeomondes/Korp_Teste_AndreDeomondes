import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, catchError, map, of } from 'rxjs';

import { API, ServiceName } from './api-config';

export type HealthState = 'checking' | 'online' | 'offline';

export interface ServiceHealth {
  service: ServiceName;
  label: string;
  state: HealthState;
}

/** Os serviços cujo estado a aplicação acompanha, na ordem em que aparecem. */
export const SERVICOS: readonly ServiceName[] = ['estoque', 'faturamento'];

const LABELS: Record<ServiceName, string> = {
  estoque: 'Serviço de Estoque',
  faturamento: 'Serviço de Faturamento',
};

export function rotuloDoServico(service: ServiceName): string {
  return LABELS[service];
}

@Injectable({ providedIn: 'root' })
export class HealthService {
  private readonly http = inject(HttpClient);

  /**
   * Um serviço fora do ar não é um erro da aplicação — é um estado que a tela
   * precisa saber exibir. Por isso a falha é convertida em `offline` em vez de
   * propagar, mantendo o fluxo vivo para os demais serviços.
   */
  check(service: ServiceName): Observable<ServiceHealth> {
    return this.http.get<{ service: string; status: string }>(`${API[service]}/health`).pipe(
      map(() => this.result(service, 'online')),
      catchError(() => of(this.result(service, 'offline'))),
    );
  }

  private result(service: ServiceName, state: HealthState): ServiceHealth {
    return { service, label: LABELS[service], state };
  }
}
