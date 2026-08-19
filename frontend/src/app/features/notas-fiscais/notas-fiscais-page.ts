import { Component, OnInit, inject, signal } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';

import { mensagemDoErro } from '../../core/api-error';
import { NotaFiscal, NotaFiscalService } from '../../core/nota-fiscal.service';

@Component({
  selector: 'app-notas-fiscais-page',
  imports: [
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './notas-fiscais-page.html',
  styleUrl: './notas-fiscais-page.scss',
})
export class NotasFiscaisPage implements OnInit {
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly colunas = ['numero', 'status'] as const;

  protected readonly notas = signal<NotaFiscal[]>([]);
  protected readonly carregando = signal(false);
  protected readonly criando = signal(false);
  protected readonly erroCriacao = signal<string | null>(null);
  protected readonly erroListagem = signal<string | null>(null);

  /** Ciclo de vida: a listagem é carregada assim que a tela entra em cena. */
  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroListagem.set(null);

    this.notaFiscalService.listar().subscribe({
      next: (notas) => {
        this.notas.set(notas);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erroListagem.set(mensagemDoErro(erro, 'Não foi possível carregar as Notas Fiscais.'));
        this.carregando.set(false);
      },
    });
  }

  protected criar(): void {
    this.criando.set(true);
    this.erroCriacao.set(null);

    this.notaFiscalService.criar().subscribe({
      next: (nota) => {
        this.criando.set(false);
        this.snackBar.open(`Nota Fiscal ${nota.numero} criada.`, 'Fechar', { duration: 4000 });
        // Relê a listagem do serviço: o que a tela mostra é o que está gravado.
        this.carregar();
      },
      error: (erro) => {
        this.erroCriacao.set(mensagemDoErro(erro, 'Não foi possível criar a Nota Fiscal.'));
        this.criando.set(false);
      },
    });
  }
}
