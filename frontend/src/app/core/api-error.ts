import { HttpErrorResponse } from '@angular/common/http';

/**
 * Converte a falha HTTP na frase que o operador lê na tela. Os serviços
 * respondem em ProblemDetails (RFC 9457), então a mensagem útil está em
 * `detail` ou, quando é erro de validação por campo, em `errors`.
 */
export function mensagemDoErro(erro: unknown, fallback: string): string {
  if (!(erro instanceof HttpErrorResponse)) {
    return fallback;
  }

  // status 0 = a requisição nem chegou ao serviço (rede caída, serviço fora).
  if (erro.status === 0) {
    return 'Serviço indisponível. Tente novamente em instantes.';
  }

  const corpo = erro.error as { detail?: string; errors?: Record<string, string[]> } | null;

  if (corpo?.errors) {
    const mensagens = Object.values(corpo.errors).flat();
    if (mensagens.length > 0) {
      return mensagens.join(' ');
    }
  }

  return corpo?.detail ?? fallback;
}
