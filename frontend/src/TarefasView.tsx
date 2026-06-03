import { useCallback, useEffect, useState, type DragEvent, type FormEvent } from 'react'
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
const COLUNAS: StatusTarefa[] = ['Pendente', 'EmAndamento', 'Concluida']

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

function formatarData(iso: string): string {
  const [ano, mes, dia] = iso.slice(0, 10).split('-')
  return `${dia}/${mes}/${ano}`
}

function PrioridadeBadge({ prioridade }: { prioridade: Prioridade }) {
  return <span className={`badge ${prioridade.toLowerCase()}`}>{PRIORIDADE_LABEL[prioridade]}</span>
}

export function TarefasView() {
  const toast = useToast()
  const [tarefas, setTarefas] = useState<Tarefa[]>([])
  const [fPrioridade, setFPrioridade] = useState('')
  const [busca, setBusca] = useState('')
  const [carregando, setCarregando] = useState(false)
  const [edicao, setEdicao] = useState<Edicao | null>(null)
  const [salvando, setSalvando] = useState(false)
  const [arrastandoId, setArrastandoId] = useState<number | null>(null)
  const [colunaAlvo, setColunaAlvo] = useState<StatusTarefa | null>(null)

  const carregar = useCallback(async () => {
    setCarregando(true)
    try {
      const params = new URLSearchParams({ pagina: '1', tamanhoPagina: '100' })
      if (fPrioridade) params.set('prioridade', fPrioridade)
      if (busca) params.set('busca', busca)
      const r = await api.listar(params)
      setTarefas(r.itens)
    } catch (ex) {
      toast.erro((ex as Error).message)
    } finally {
      setCarregando(false)
    }
  }, [fPrioridade, busca, toast])

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

  async function moverStatus(tarefa: Tarefa, novoStatus: StatusTarefa) {
    const anterior = tarefas
    // Atualização otimista: move o card na hora.
    setTarefas(ts => ts.map(t => (t.id === tarefa.id ? { ...t, status: novoStatus } : t)))
    try {
      await api.atualizar(tarefa.id, {
        titulo: tarefa.titulo,
        descricao: tarefa.descricao,
        dataVencimento: tarefa.dataVencimento.slice(0, 10),
        status: novoStatus,
        prioridade: tarefa.prioridade
      })
    } catch (ex) {
      setTarefas(anterior) // reverte em caso de erro
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

  function aoSoltar(e: DragEvent, status: StatusTarefa) {
    e.preventDefault()
    setColunaAlvo(null)
    const id = Number(e.dataTransfer.getData('text/plain'))
    const tarefa = tarefas.find(t => t.id === id)
    if (tarefa && tarefa.status !== status) void moverStatus(tarefa, status)
  }

  return (
    <section>
      <div className="filtros">
        <input
          placeholder="Buscar por título..."
          value={busca}
          onChange={e => setBusca(e.target.value)}
        />
        <select value={fPrioridade} onChange={e => setFPrioridade(e.target.value)}>
          <option value="">Prioridade (todas)</option>
          {PRIORIDADES.map(p => <option key={p} value={p}>{PRIORIDADE_LABEL[p]}</option>)}
        </select>
        <button className="novo" onClick={() => setEdicao(novaEdicao())}>+ Nova tarefa</button>
      </div>

      {carregando ? (
        <p className="carregando">Carregando...</p>
      ) : (
        <div className="kanban">
          {COLUNAS.map(status => {
            const itens = tarefas.filter(t => t.status === status)
            return (
              <div
                key={status}
                className={`coluna ${colunaAlvo === status ? 'dragover' : ''}`}
                onDragOver={e => { e.preventDefault(); setColunaAlvo(status) }}
                onDragLeave={() => setColunaAlvo(c => (c === status ? null : c))}
                onDrop={e => aoSoltar(e, status)}
              >
                <div className="coluna-titulo">
                  <span>{STATUS_LABEL[status]}</span>
                  <span className="contagem">{itens.length}</span>
                </div>

                {itens.length === 0 && <div className="coluna-vazia">Sem tarefas</div>}

                {itens.map(t => (
                  <div
                    key={t.id}
                    className={`kanban-card ${arrastandoId === t.id ? 'arrastando' : ''}`}
                    draggable
                    onDragStart={e => { e.dataTransfer.setData('text/plain', String(t.id)); setArrastandoId(t.id) }}
                    onDragEnd={() => setArrastandoId(null)}
                  >
                    <div className="titulo">{t.titulo}</div>
                    {t.descricao && <div className="desc">{t.descricao}</div>}
                    <div className="meta">
                      <span className="venc">📅 {formatarData(t.dataVencimento)}</span>
                      <PrioridadeBadge prioridade={t.prioridade} />
                    </div>
                    <div className="acoes">
                      <button onClick={() => editar(t)}>Editar</button>
                      <button className="perigo" onClick={() => remover(t.id)}>Excluir</button>
                    </div>
                  </div>
                ))}
              </div>
            )
          })}
        </div>
      )}

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
