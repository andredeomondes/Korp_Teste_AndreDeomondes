import { HttpErrorResponse } from '@angular/common/http';

/**
 * Por que a operação não deu certo. A distinção importa para o operador: um
 * erro de negócio pede que ele mude alguma coisa; uma indisponibilidade pede
 * apenas que ele tente de novo daqui a pouco.
 */
export type TipoDeFalha = 'negocio' | 'indisponibilidade' | 'desconhecida';

export interface FalhaDaApi {
  tipo: TipoDeFalha;
  mensagem: string;
  /** Identificador estável da recusa, quando o serviço o informa. */
  codigo?: string;
}

/** Uma recusa que a própria tela decidiu, sem ida ao servidor. */
export function falhaDeNegocio(mensagem: string): FalhaDaApi {
  return { tipo: 'negocio', mensagem };
}

/**
 * Converte a falha HTTP no que o operador lê na tela. Os serviços respondem em
 * ProblemDetails (RFC 9457), então a mensagem útil está em `detail` ou, quando
 * é erro de validação por campo, em `errors`.
 */
export function falhaDaApi(erro: unknown, fallback: string): FalhaDaApi {
  if (!(erro instanceof HttpErrorResponse)) {
    return { tipo: 'desconhecida', mensagem: fallback };
  }

  const corpo = erro.error as {
    detail?: string;
    codigo?: string;
    errors?: Record<string, string[]>;
  } | null;

  // status 0 = a requisição nem chegou ao serviço (rede caída, serviço fora).
  // 503 é a indisponibilidade que o próprio serviço reconhece e traduz — por
  // exemplo, o Faturamento avisando que não alcançou o Estoque.
  if (erro.status === 0 || erro.status === 503) {
    return {
      tipo: 'indisponibilidade',
      mensagem: corpo?.detail ?? 'Serviço indisponível. Tente novamente em instantes.',
    };
  }

  // 4xx é recusa de negócio: o operador precisa mudar alguma coisa. Fora dessa
  // faixa não é decisão do domínio, por mais estruturado que venha o corpo.
  if (erro.status >= 400 && erro.status < 500) {
    const porCampo = Object.values(corpo?.errors ?? {}).flat();

    if (porCampo.length > 0) {
      return { tipo: 'negocio', mensagem: porCampo.join(' ') };
    }

    if (corpo?.detail) {
      return { tipo: 'negocio', mensagem: corpo.detail, codigo: corpo.codigo };
    }
  }

  return { tipo: 'desconhecida', mensagem: corpo?.detail ?? fallback };
}

/** Só a frase, para telas que não distinguem os casos. */
export function mensagemDoErro(erro: unknown, fallback: string): string {
  return falhaDaApi(erro, fallback).mensagem;
}
