import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'produtos' },
  {
    path: 'produtos',
    title: 'Produtos',
    loadComponent: () => import('./features/produtos/produtos-page').then((m) => m.ProdutosPage),
  },
  {
    path: 'notas-fiscais',
    title: 'Notas Fiscais',
    loadComponent: () =>
      import('./features/notas-fiscais/notas-fiscais-page').then((m) => m.NotasFiscaisPage),
  },
  {
    path: 'notas-fiscais/:id',
    title: 'Nota Fiscal',
    loadComponent: () =>
      import('./features/notas-fiscais/nota-fiscal-detalhe-page').then(
        (m) => m.NotaFiscalDetalhePage,
      ),
  },
  {
    path: 'status',
    title: 'Status dos serviços',
    loadComponent: () => import('./features/status/status-page').then((m) => m.StatusPage),
  },
  { path: '**', redirectTo: 'produtos' },
];
