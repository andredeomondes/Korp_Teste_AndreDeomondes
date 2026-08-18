/**
 * Endereços dos microsserviços. Em desenvolvimento apontam para as portas
 * fixas definidas nos launchSettings de cada serviço; no docker-compose são
 * sobrescritos pelas variáveis de ambiente injetadas no build.
 */
export const API = {
  estoque: 'http://localhost:5001',
  faturamento: 'http://localhost:5002',
} as const;

export type ServiceName = keyof typeof API;
