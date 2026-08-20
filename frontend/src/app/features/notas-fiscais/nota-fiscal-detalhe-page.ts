import { Component, OnInit, inject, input, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';

import { mensagemDoErro } from '../../core/api-error';
import { NotaFiscal, NotaFiscalService, ProdutoDisponivel } from '../../core/nota-fiscal.service';

@Component({
  selector: 'app-nota-fiscal-detalhe-page',
  imports: [
    ReactiveFormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    MatTableModule,
  ],
  templateUrl: './nota-fiscal-detalhe-page.html',
  styleUrl: './nota-fiscal-detalhe-page.scss',
})
export class NotaFiscalDetalhePage implements OnInit {
  private readonly notaFiscalService = inject(NotaFiscalService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly formBuilder = inject(FormBuilder);

  /** Vem da rota `/notas-fiscais/:id`, ligada por `withComponentInputBinding`. */
  readonly id = input.required<string>();

  protected readonly colunas = ['codigo', 'descricao', 'quantidade', 'saldo', 'acoes'] as const;

  protected readonly nota = signal<NotaFiscal | null>(null);
  protected readonly produtos = signal<ProdutoDisponivel[]>([]);
  protected readonly carregando = signal(false);
  protected readonly salvando = signal(false);
  protected readonly imprimindo = signal(false);
  protected readonly erro = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    produtoId: ['', Validators.required],
    quantidade: [1, [Validators.required, Validators.min(1)]],
  });

  ngOnInit(): void {
    this.carregar();
    this.carregarProdutos();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erro.set(null);

    this.notaFiscalService.obter(this.id()).subscribe({
      next: (nota) => {
        this.nota.set(nota);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erro.set(mensagemDoErro(erro, 'Não foi possível carregar a Nota Fiscal.'));
        this.carregando.set(false);
      },
    });
  }

  protected adicionar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const { produtoId, quantidade } = this.form.getRawValue();
    this.salvando.set(true);
    this.erro.set(null);

    this.notaFiscalService.adicionarItem(this.id(), produtoId, quantidade).subscribe({
      next: () => {
        this.form.reset({ produtoId: '', quantidade: 1 });
        this.salvando.set(false);
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemDoErro(erro, 'Não foi possível adicionar o Item da Nota.'));
        this.salvando.set(false);
      },
    });
  }

  protected alterarQuantidade(itemId: string, valor: string): void {
    const quantidade = Number(valor);

    if (!Number.isInteger(quantidade) || quantidade < 1) {
      this.erro.set('Quantidade do Item da Nota deve ser positiva.');
      return;
    }

    this.erro.set(null);

    this.notaFiscalService.alterarQuantidade(this.id(), itemId, quantidade).subscribe({
      next: () => this.carregar(),
      error: (erro) => {
        this.erro.set(mensagemDoErro(erro, 'Não foi possível alterar a quantidade.'));
        this.carregar();
      },
    });
  }

  /**
   * Saldo que o Produto tem hoje no Estoque. Depois da Impressão ele aparece já
   * debitado — é como a tela mostra que a baixa aconteceu.
   */
  protected saldoDe(produtoId: string): number | null {
    return this.produtos().find((produto) => produto.id === produtoId)?.saldo ?? null;
  }

  /**
   * A Impressão pode demorar: ela atravessa o Faturamento e o Estoque. Enquanto
   * corre, o botão fica desabilitado e a tela mostra que está processando —
   * clicar duas vezes tentaria debitar o Saldo duas vezes.
   */
  protected imprimir(): void {
    this.imprimindo.set(true);
    this.erro.set(null);

    this.notaFiscalService.imprimir(this.id()).subscribe({
      next: (nota) => {
        this.nota.set(nota);
        this.imprimindo.set(false);
        this.snackBar.open(`Nota Fiscal ${nota.numero} impressa.`, 'Fechar', { duration: 4000 });
        // O Saldo dos Produtos acabou de mudar: a tela precisa mostrar o novo.
        this.carregarProdutos();
      },
      error: (erro) => {
        this.erro.set(mensagemDoErro(erro, 'Não foi possível imprimir a Nota Fiscal.'));
        this.imprimindo.set(false);
        // A nota pode ter mudado no servidor; a tela não adivinha o estado dela.
        this.carregar();
      },
    });
  }

  protected remover(itemId: string): void {
    this.erro.set(null);

    this.notaFiscalService.removerItem(this.id(), itemId).subscribe({
      next: () => {
        this.snackBar.open('Item da Nota removido.', 'Fechar', { duration: 3000 });
        this.carregar();
      },
      error: (erro) => {
        this.erro.set(mensagemDoErro(erro, 'Não foi possível remover o Item da Nota.'));
      },
    });
  }

  /**
   * A lista de Produtos vem do Estoque, que é o dono dela. Uma falha aqui não
   * impede ver a Nota Fiscal — apenas deixa o operador sem o que adicionar.
   */
  private carregarProdutos(): void {
    this.notaFiscalService.produtosDisponiveis().subscribe({
      next: (produtos) => this.produtos.set(produtos),
      error: (erro) =>
        this.erro.set(mensagemDoErro(erro, 'Não foi possível carregar os Produtos do Estoque.')),
    });
  }
}
