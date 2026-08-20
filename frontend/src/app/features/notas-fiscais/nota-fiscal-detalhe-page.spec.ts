import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { provideRouter } from '@angular/router';

import { NotaFiscalDetalhePage } from './nota-fiscal-detalhe-page';
import { API } from '../../core/api-config';
import { NotaFiscal, StatusNotaFiscal } from '../../core/nota-fiscal.service';

const NOTA_ID = '11111111-1111-1111-1111-111111111111';

describe('NotaFiscalDetalhePage', () => {
  let http: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [NotaFiscalDetalhePage],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        // A tela tem um link de volta para a listagem.
        provideRouter([]),
      ],
    }).compileComponents();

    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  /** Monta a tela para uma Nota Fiscal com o Status pedido e devolve o HTML. */
  async function renderizar(status: StatusNotaFiscal): Promise<HTMLElement> {
    const fixture = TestBed.createComponent(NotaFiscalDetalhePage);
    fixture.componentRef.setInput('id', NOTA_ID);
    fixture.detectChanges();

    const nota: NotaFiscal = {
      id: NOTA_ID,
      numero: 42,
      status,
      itens: [
        {
          id: '22222222-2222-2222-2222-222222222222',
          produtoId: '33333333-3333-3333-3333-333333333333',
          codigo: 'CAF-001',
          descricao: 'Café em grãos 1kg',
          quantidade: 2,
        },
      ],
    };

    http.expectOne(`${API.faturamento}/notas-fiscais/${NOTA_ID}`).flush(nota);
    http.expectOne(`${API.faturamento}/produtos-disponiveis`).flush([]);

    await fixture.whenStable();
    fixture.detectChanges();

    return fixture.nativeElement as HTMLElement;
  }

  it('oferece Imprimir e edição enquanto a Nota Fiscal está Aberta', async () => {
    const tela = await renderizar('Aberta');

    expect(tela.textContent).toContain('Imprimir');
    expect(tela.querySelector('form')).not.toBeNull();
    expect(tela.querySelector('button[aria-label^="Remover"]')).not.toBeNull();
  });

  it('renderiza a Nota Fiscal Fechada somente leitura, sem botão de impressão', async () => {
    const tela = await renderizar('Fechada');

    expect(tela.textContent).not.toContain('Imprimir');
    expect(tela.querySelector('form')).toBeNull();
    expect(tela.querySelector('button[aria-label^="Remover"]')).toBeNull();

    // Os itens continuam visíveis — só não há como mexer neles.
    expect(tela.textContent).toContain('CAF-001');
    expect(tela.querySelector<HTMLInputElement>('.detalhe__quantidade')?.disabled).toBe(true);
  });
});
