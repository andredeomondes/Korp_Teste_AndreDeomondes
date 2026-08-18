import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatChipsModule } from '@angular/material/chips';

import { HealthService, ServiceHealth } from './core/health.service';
import { ServiceName } from './core/api-config';

const SERVICES: ServiceName[] = ['estoque', 'faturamento'];

const LABELS: Record<ServiceName, string> = {
  estoque: 'Serviço de Estoque',
  faturamento: 'Serviço de Faturamento',
};

@Component({
  selector: 'app-root',
  imports: [
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatChipsModule,
  ],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App implements OnInit {
  private readonly health = inject(HealthService);

  protected readonly services = signal<ServiceHealth[]>([]);
  protected readonly checking = signal(false);

  ngOnInit(): void {
    this.refresh();
  }

  protected refresh(): void {
    this.checking.set(true);
    this.services.set(
      SERVICES.map((service) => ({ service, label: LABELS[service], state: 'checking' })),
    );

    let pending = SERVICES.length;
    for (const service of SERVICES) {
      this.health.check(service).subscribe((result) => {
        this.services.update((current) =>
          current.map((item) => (item.service === result.service ? result : item)),
        );
        if (--pending === 0) {
          this.checking.set(false);
        }
      });
    }
  }

  protected iconOf(state: ServiceHealth['state']): string {
    return state === 'online' ? 'check_circle' : 'cancel';
  }

  protected textOf(state: ServiceHealth['state']): string {
    if (state === 'online') return 'No ar';
    if (state === 'offline') return 'Indisponível';
    return 'Verificando';
  }
}
