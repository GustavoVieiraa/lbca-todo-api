export type StatusTarefa = 'Pendente' | 'EmAndamento' | 'Concluida'
export type Prioridade = 'Baixa' | 'Media' | 'Alta'

export interface Tarefa {
  id: number
  titulo: string
  descricao: string | null
  dataVencimento: string
  status: StatusTarefa
  prioridade: Prioridade
  criadoEm: string
  atualizadoEm: string | null
}

export interface PagedResult<T> {
  itens: T[]
  pagina: number
  tamanhoPagina: number
  totalItens: number
  totalPaginas: number
}

export interface CriarTarefa {
  titulo: string
  descricao: string | null
  dataVencimento: string
  prioridade: Prioridade
}

export interface AtualizarTarefa extends CriarTarefa {
  status: StatusTarefa
}

export interface ErroImportacao {
  linha: number
  coluna: string
  valor: string | null
  erro: string
}

export interface ImportacaoResultado {
  nomeArquivo: string
  totalLinhas: number
  importadas: number
  falhas: number
  erros: ErroImportacao[]
}
