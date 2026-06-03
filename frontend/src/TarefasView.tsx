import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from './api'
import { useToast } from './toast'
import {
  PRIORIDADE_LABEL,
  STATUS_LABEL,
  type Prioridade,
  type StatusTarefa,
  type Tarefa
} from './types'

const STATUS: StatusTarefa[] = ['Pendente', 'EmAndamento', 'Concluida']
const PRIORIDADES: Prioridade[] = ['Baixa', 'Media', 'Alta']

interface Edicao {
  id: number
  titulo: string
  descricao: string
  dataVencimento: string
  status: StatusTarefa
  prioridade: Prioridade
}

const novaEdicao = (): Edicao => ({
  id: 0, titulo: '', descricao: '', dataVencimento: '', status: 'Pendente', prioridade: 'Media'
})

function StatusBadge({ status }: { status: StatusTarefa }) {
  return <span className={`badge ${status.toLowerCase()}`}>{STATUS_LABEL[status]}</span>
}

function PrioridadeBadge({ prioridade }: { prioridade: Prioridade }) {
  return <span className={`badge ${prioridade.toLowerCase()}`}>{PRIORIDADE_LABEL[prioridade]}</span>
}

export function TarefasView() {
  const toast = useToast()
  const [tarefas, setTarefas] = useState<Tarefa[]>([])
  const [pagina, setPagina] = useState(1)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [fStatus, setFStatus] = useState('')
  const [fPrioridade, setFPrioridade] = useState('')
  const [busca, setBusca] = useState('')
  const [carregando, setCarregando] = useState(false)
  const [edicao, setEdicao] = useState<Edicao | null>(null)
  const [salvando, setSalvando] = useState(false)

  const carregar = useCallback(async () => {
    setCarregando(true)
    try {
      const params = new URLSearchParams({ pagina: String(pagina), tamanhoPagina: '10' })
      if (fStatus) params.set('status', fStatus)
      if (fPrioridade) params.set('prioridade', fPrioridade)
      if (busca) params.set('busca', busca)
      const r = await api.listar(params)
      setTarefas(r.itens)
      setTotalPaginas(r.totalPaginas || 1)
    } catch (ex) {
      toast.erro((ex as Error).message)
    } finally {
      setCarregando(false)
    }
  }, [pagina, fStatus, fPrioridade, busca, toast])

  useEffect(() => { void carregar() }, [carregar])

  async function salvar(e: FormEvent) {
    e.preventDefault()
    if (!edicao) return
    setSalvando(true)
    try {
      const base = {
        titulo: edicao.titulo,
        descricao: edicao.descricao || null,
        dataVencimento: edicao.dataVencimento,
        prioridade: edicao.prioridade
      }
      if (edicao.id === 0) {
        await api.criar(base)
        toast.sucesso('Tarefa criada com sucesso.')
      } else {
        await api.atualizar(edicao.id, { ...base, status: edicao.status })
        toast.sucesso('Tarefa atualizada com sucesso.')
      }
      setEdicao(null)
      await carregar()
    } catch (ex) {
      toast.erro((ex as Error).message)
    } finally {
      setSalvando(false)
    }
  }

  async function remover(id: number) {
    if (!window.confirm('Remover esta tarefa?')) return
    try {
      await api.remover(id)
      toast.sucesso('Tarefa removida.')
      await carregar()
    } catch (ex) {
      toast.erro((ex as Error).message)
    }
  }

  function editar(t: Tarefa) {
    setEdicao({
      id: t.id,
      titulo: t.titulo,
      descricao: t.descricao ?? '',
      dataVencimento: t.dataVencimento.slice(0, 10),
      status: t.status,
      prioridade: t.prioridade
    })
  }

  return (
    <section>
      <div className="filtros">
        <input
          placeholder="Buscar por título..."
          value={busca}
          onChange={e => { setPagina(1); setBusca(e.target.value) }}
        />
        <select value={fStatus} onChange={e => { setPagina(1); setFStatus(e.target.value) }}>
          <option value="">Status (todos)</option>
          {STATUS.map(s => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
        </select>
        <select value={fPrioridade} onChange={e => { setPagina(1); setFPrioridade(e.target.value) }}>
          <option value="">Prioridade (todas)</option>
          {PRIORIDADES.map(p => <option key={p} value={p}>{PRIORIDADE_LABEL[p]}</option>)}
        </select>
        <button className="novo" onClick={() => setEdicao(novaEdicao())}>+ Nova tarefa</button>
      </div>

      <table>
        <thead>
          <tr><th>Título</th><th>Vencimento</th><th>Status</th><th>Prioridade</th><th></th></tr>
        </thead>
        <tbody>
          {carregando ? (
            <tr><td colSpan={5} className="carregando">Carregando...</td></tr>
          ) : tarefas.length === 0 ? (
            <tr><td colSpan={5} className="vazio">Nenhuma tarefa encontrada.</td></tr>
          ) : (
            tarefas.map(t => (
              <tr key={t.id}>
                <td>{t.titulo}</td>
                <td>{t.dataVencimento.slice(0, 10)}</td>
                <td><StatusBadge status={t.status} /></td>
                <td><PrioridadeBadge prioridade={t.prioridade} /></td>
                <td className="acoes">
                  <button onClick={() => editar(t)}>Editar</button>
                  <button className="perigo" onClick={() => remover(t.id)}>Excluir</button>
                </td>
              </tr>
            ))
          )}
        </tbody>
      </table>

      <div className="paginacao">
        <button disabled={pagina <= 1} onClick={() => setPagina(p => p - 1)}>← Anterior</button>
        <span>Página {pagina} de {totalPaginas}</span>
        <button disabled={pagina >= totalPaginas} onClick={() => setPagina(p => p + 1)}>Próxima →</button>
      </div>

      {edicao && (
        <div className="modal" onClick={() => setEdicao(null)}>
          <form className="card" onClick={e => e.stopPropagation()} onSubmit={salvar}>
            <h2>{edicao.id === 0 ? 'Nova tarefa' : 'Editar tarefa'}</h2>
            <label>
              Título
              <input
                required maxLength={100}
                value={edicao.titulo}
                onChange={e => setEdicao({ ...edicao, titulo: e.target.value })}
              />
            </label>
            <label>
              Descrição
              <textarea
                value={edicao.descricao}
                onChange={e => setEdicao({ ...edicao, descricao: e.target.value })}
              />
            </label>
            <label>
              Data de vencimento
              <input
                type="date" required
                value={edicao.dataVencimento}
                onChange={e => setEdicao({ ...edicao, dataVencimento: e.target.value })}
              />
            </label>
            <label>
              Prioridade
              <select
                value={edicao.prioridade}
                onChange={e => setEdicao({ ...edicao, prioridade: e.target.value as Prioridade })}
              >
                {PRIORIDADES.map(p => <option key={p} value={p}>{PRIORIDADE_LABEL[p]}</option>)}
              </select>
            </label>
            {edicao.id !== 0 && (
              <label>
                Status
                <select
                  value={edicao.status}
                  onChange={e => setEdicao({ ...edicao, status: e.target.value as StatusTarefa })}
                >
                  {STATUS.map(s => <option key={s} value={s}>{STATUS_LABEL[s]}</option>)}
                </select>
              </label>
            )}
            <div className="acoes">
              <button type="button" onClick={() => setEdicao(null)}>Cancelar</button>
              <button type="submit" className="primario" disabled={salvando}>
                {salvando ? 'Salvando...' : 'Salvar'}
              </button>
            </div>
          </form>
        </div>
      )}
    </section>
  )
}
