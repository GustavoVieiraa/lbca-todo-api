import type { AtualizarTarefa, CriarTarefa, ImportacaoResultado, PagedResult, Tarefa } from './types'

const TOKEN_KEY = 'todoapp.token'

export const auth = {
  get token(): string | null {
    return localStorage.getItem(TOKEN_KEY)
  },
  set(token: string): void {
    localStorage.setItem(TOKEN_KEY, token)
  },
  clear(): void {
    localStorage.removeItem(TOKEN_KEY)
  },
  get autenticado(): boolean {
    return !!localStorage.getItem(TOKEN_KEY)
  }
}

async function request<T>(url: string, options: RequestInit = {}): Promise<T> {
  const headers = new Headers(options.headers)
  if (auth.token) headers.set('Authorization', `Bearer ${auth.token}`)
  if (options.body && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }

  const resposta = await fetch(url, { ...options, headers })

  if (resposta.status === 401) {
    auth.clear()
    throw new Error('Sessão expirada ou não autorizada. Faça login novamente.')
  }

  if (!resposta.ok) {
    const problema = await resposta.json().catch(() => null)
    const mensagem = problema?.errors
      ? Object.values(problema.errors as Record<string, string[]>).flat().join(' ')
      : (problema?.detail ?? problema?.title ?? `Erro ${resposta.status}`)
    throw new Error(mensagem)
  }

  if (resposta.status === 204) return undefined as T
  return resposta.json() as Promise<T>
}

export const api = {
  async login(usuario: string, senha: string): Promise<void> {
    const r = await request<{ token: string }>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ usuario, senha })
    })
    auth.set(r.token)
  },

  listar(params: URLSearchParams): Promise<PagedResult<Tarefa>> {
    return request<PagedResult<Tarefa>>(`/api/tarefas?${params.toString()}`)
  },

  criar(tarefa: CriarTarefa): Promise<Tarefa> {
    return request<Tarefa>('/api/tarefas', { method: 'POST', body: JSON.stringify(tarefa) })
  },

  atualizar(id: number, tarefa: AtualizarTarefa): Promise<void> {
    return request<void>(`/api/tarefas/${id}`, { method: 'PUT', body: JSON.stringify(tarefa) })
  },

  remover(id: number): Promise<void> {
    return request<void>(`/api/tarefas/${id}`, { method: 'DELETE' })
  },

  importar(arquivo: File): Promise<ImportacaoResultado> {
    const form = new FormData()
    form.append('arquivo', arquivo)
    return request<ImportacaoResultado>('/api/tarefas/importar', { method: 'POST', body: form })
  },

  async baixarModelo(tipo: 'completo' | 'erros'): Promise<Blob> {
    const headers = new Headers()
    if (auth.token) headers.set('Authorization', `Bearer ${auth.token}`)

    const resposta = await fetch(`/api/tarefas/modelo?tipo=${tipo}`, { headers })
    if (resposta.status === 401) {
      auth.clear()
      throw new Error('Sessão expirada. Faça login novamente.')
    }
    if (!resposta.ok) throw new Error('Não foi possível baixar a planilha de exemplo.')

    return resposta.blob()
  }
}
