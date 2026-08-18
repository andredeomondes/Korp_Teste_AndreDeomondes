import { Component, OnInit, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { HealthService, SERVICOS, ServiceHealth, rotuloDoServico } from '../../core/health.service';

@Component({
  selector: 'app-status-page',
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressSpinnerModule],
  templateUrl: './status-page.html',
  styleUrl: './status-page.scss',
})
export class StatusPage implements OnInit {
  private readonly health = inject(HealthService);

  protected readonly services = signal<ServiceHealth[]>([]);
  protected readonly checking = signal(false);

  ngOnInit(): void {
    this.refresh();
  }

  protected refresh(): void {
    this.checking.set(true);
    this.services.set(
      SERVICOS.map((service) => ({ service, label: rotuloDoServico(service), state: 'checking' })),
    );

    let pending = SERVICOS.length;
    for (const service of SERVICOS) {
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
