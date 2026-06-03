import { useCallback, useEffect, useState, type FormEvent } from 'react'
import { api } from './api'
import type { Prioridade, StatusTarefa, Tarefa } from './types'

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

export function TarefasView() {
  const [tarefas, setTarefas] = useState<Tarefa[]>([])
  const [pagina, setPagina] = useState(1)
  const [totalPaginas, setTotalPaginas] = useState(1)
  const [fStatus, setFStatus] = useState('')
  const [fPrioridade, setFPrioridade] = useState('')
  const [busca, setBusca] = useState('')
  const [erro, setErro] = useState<string | null>(null)
  const [edicao, setEdicao] = useState<Edicao | null>(null)

  const carregar = useCallback(async () => {
    setErro(null)
    try {
      const params = new URLSearchParams({ pagina: String(pagina), tamanhoPagina: '10' })
      if (fStatus) params.set('status', fStatus)
      if (fPrioridade) params.set('prioridade', fPrioridade)
      if (busca) params.set('busca', busca)
      const r = await api.listar(params)
      setTarefas(r.itens)
      setTotalPaginas(r.totalPaginas || 1)
    } catch (ex) {
      setErro((ex as Error).message)
    }
  }, [pagina, fStatus, fPrioridade, busca])

  useEffect(() => { void carregar() }, [carregar])

  async function salvar(e: FormEvent) {
    e.preventDefault()
    if (!edicao) return
    setErro(null)
    try {
      const base = {
        titulo: edicao.titulo,
        descricao: edicao.descricao || null,
        dataVencimento: edicao.dataVencimento,
        prioridade: edicao.prioridade
      }
      if (edicao.id === 0) {
        await api.criar(base)
      } else {
        await api.atualizar(edicao.id, { ...base, status: edicao.status })
      }
      setEdicao(null)
      await carregar()
    } catch (ex) {
      setErro((ex as Error).message)
    }
  }

  async function remover(id: number) {
    if (!window.confirm('Remover esta tarefa?')) return
    setErro(null)
    try {
      await api.remover(id)
      await carregar()
    } catch (ex) {
      setErro((ex as Error).message)
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
          {STATUS.map(s => <option key={s} value={s}>{s}</option>)}
        </select>
        <select value={fPrioridade} onChange={e => { setPagina(1); setFPrioridade(e.target.value) }}>
          <option value="">Prioridade (todas)</option>
          {PRIORIDADES.map(p => <option key={p} value={p}>{p}</option>)}
        </select>
        <button onClick={() => setEdicao(novaEdicao())}>+ Nova tarefa</button>
      </div>

      {erro && <p className="erro">{erro}</p>}

      <table>
        <thead>
          <tr><th>Título</th><th>Vencimento</th><th>Status</th><th>Prioridade</th><th></th></tr>
        </thead>
        <tbody>
          {tarefas.map(t => (
            <tr key={t.id}>
              <td>{t.titulo}</td>
              <td>{t.dataVencimento.slice(0, 10)}</td>
              <td>{t.status}</td>
              <td>{t.prioridade}</td>
              <td className="acoes">
                <button onClick={() => editar(t)}>Editar</button>
                <button className="perigo" onClick={() => remover(t.id)}>Excluir</button>
              </td>
            </tr>
          ))}
          {tarefas.length === 0 && (
            <tr><td colSpan={5} className="vazio">Nenhuma tarefa encontrada.</td></tr>
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
                {PRIORIDADES.map(p => <option key={p} value={p}>{p}</option>)}
              </select>
            </label>
            {edicao.id !== 0 && (
              <label>
                Status
                <select
                  value={edicao.status}
                  onChange={e => setEdicao({ ...edicao, status: e.target.value as StatusTarefa })}
                >
                  {STATUS.map(s => <option key={s} value={s}>{s}</option>)}
                </select>
              </label>
            )}
            <div className="acoes">
              <button type="button" onClick={() => setEdicao(null)}>Cancelar</button>
              <button type="submit">Salvar</button>
            </div>
          </form>
        </div>
      )}
    </section>
  )
}
