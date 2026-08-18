import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'produtos' },
  {
    path: 'produtos',
    title: 'Produtos',
    loadComponent: () => import('./features/produtos/produtos-page').then((m) => m.ProdutosPage),
  },
  {
    path: 'status',
    title: 'Status dos serviços',
    loadComponent: () => import('./features/status/status-page').then((m) => m.StatusPage),
  },
  { path: '**', redirectTo: 'produtos' },
];
