import { Component, OnInit, inject, signal } from '@angular/core';
import {
  AbstractControl,
  FormBuilder,
  ReactiveFormsModule,
  ValidationErrors,
  Validators,
} from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';

import { mensagemDoErro } from '../../core/api-error';
import { Produto, ProdutoService } from '../../core/produto.service';

/**
 * Saldo conta unidades em estoque: 1,5 não é um Saldo possível, e o serviço o
 * recusaria com um erro de desserialização difícil de ler na tela.
 */
function saldoInteiro(controle: AbstractControl): ValidationErrors | null {
  return Number.isInteger(controle.value) ? null : { inteiro: true };
}

@Component({
  selector: 'app-produtos-page',
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
  ],
  templateUrl: './produtos-page.html',
  styleUrl: './produtos-page.scss',
})
export class ProdutosPage implements OnInit {
  private readonly produtoService = inject(ProdutoService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly colunas = ['codigo', 'descricao', 'saldo'] as const;

  protected readonly produtos = signal<Produto[]>([]);
  protected readonly carregando = signal(false);
  protected readonly salvando = signal(false);
  protected readonly erroCadastro = signal<string | null>(null);
  protected readonly erroListagem = signal<string | null>(null);

  protected readonly form = this.formBuilder.nonNullable.group({
    codigo: ['', [Validators.required, Validators.maxLength(50)]],
    descricao: ['', [Validators.required, Validators.maxLength(200)]],
    saldo: [0, [Validators.required, Validators.min(0), saldoInteiro]],
  });

  /** Ciclo de vida: a listagem é carregada assim que a tela entra em cena. */
  ngOnInit(): void {
    this.carregar();
  }

  protected carregar(): void {
    this.carregando.set(true);
    this.erroListagem.set(null);

    this.produtoService.listar().subscribe({
      next: (produtos) => {
        this.produtos.set(produtos);
        this.carregando.set(false);
      },
      error: (erro) => {
        this.erroListagem.set(mensagemDoErro(erro, 'Não foi possível carregar os Produtos.'));
        this.carregando.set(false);
      },
    });
  }

  protected cadastrar(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.salvando.set(true);
    this.erroCadastro.set(null);

    this.produtoService.cadastrar(this.form.getRawValue()).subscribe({
      next: (produto) => {
        this.form.reset({ codigo: '', descricao: '', saldo: 0 });
        this.salvando.set(false);
        this.snackBar.open(`Produto ${produto.codigo} cadastrado.`, 'Fechar', { duration: 4000 });
        // Relê a listagem do serviço em vez de inserir o Produto localmente: o
        // que a tela mostra é o que está gravado, não o que este cliente supõe.
        this.carregar();
      },
      error: (erro) => {
        this.erroCadastro.set(mensagemDoErro(erro, 'Não foi possível cadastrar o Produto.'));
        this.salvando.set(false);
      },
    });
  }
}
