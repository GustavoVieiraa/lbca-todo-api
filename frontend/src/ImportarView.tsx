import { useRef, useState, type DragEvent } from 'react'
import { api } from './api'
import { useToast } from './toast'
import type { ImportacaoResultado } from './types'

function formatarTamanho(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`
}

export function ImportarView() {
  const toast = useToast()
  const inputRef = useRef<HTMLInputElement>(null)
  const [arquivo, setArquivo] = useState<File | null>(null)
  const [resultado, setResultado] = useState<ImportacaoResultado | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [arrastando, setArrastando] = useState(false)
  const [baixando, setBaixando] = useState<'completo' | 'erros' | null>(null)

  function selecionar(file: File | null) {
    if (!file) return
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      toast.erro('Selecione um arquivo .xlsx.')
      return
    }
    setArquivo(file)
    setResultado(null)
  }

  function limpar() {
    setArquivo(null)
    setResultado(null)
    if (inputRef.current) inputRef.current.value = ''
  }

  function aoSoltar(e: DragEvent) {
    e.preventDefault()
    setArrastando(false)
    selecionar(e.dataTransfer.files?.[0] ?? null)
  }

  async function baixarExemplo(tipo: 'completo' | 'erros') {
    setBaixando(tipo)
    try {
      const blob = await api.baixarModelo(tipo)
      const nome = tipo === 'erros' ? 'tarefas-exemplo-com-erros.xlsx' : 'tarefas-exemplo.xlsx'
      const url = URL.createObjectURL(blob)
      const link = document.createElement('a')
      link.href = url
      link.download = nome
      link.click()
      URL.revokeObjectURL(url)
      toast.sucesso('Planilha de exemplo baixada.')
    } catch (ex) {
      toast.erro((ex as Error).message)
    } finally {
      setBaixando(null)
    }
  }

  async function enviar() {
    if (!arquivo) return
    setResultado(null)
    setEnviando(true)
    try {
      const r = await api.importar(arquivo)
      setResultado(r)
      if (r.falhas === 0) toast.sucesso(`${r.importadas} tarefa(s) importada(s).`)
      else toast.erro(`${r.importadas} importada(s), ${r.falhas} com falha. Veja o relatório.`)
    } catch (ex) {
      toast.erro((ex as Error).message)
    } finally {
      setEnviando(false)
    }
  }

  return (
    <section className="importar-view">
      <h2>Importar tarefas (.xlsx)</h2>
      <p className="ajuda">
        Colunas esperadas: <strong>Título</strong>, <strong>Descrição</strong>,
        {' '}<strong>Data de Vencimento</strong> e <strong>Prioridade</strong>.
        Linhas inválidas não impedem a importação — aparecem no relatório.
      </p>

      <div className="exemplos">
        <button onClick={() => baixarExemplo('completo')} disabled={baixando !== null}>
          {baixando === 'completo' ? 'Baixando...' : '⬇ Exemplo completo (10 tarefas)'}
        </button>
        <button onClick={() => baixarExemplo('erros')} disabled={baixando !== null}>
          {baixando === 'erros' ? 'Baixando...' : '⬇ Exemplo com erros'}
        </button>
      </div>

      <div
        className={`dropzone ${arrastando ? 'arrastando' : ''}`}
        onClick={() => inputRef.current?.click()}
        onDragOver={e => { e.preventDefault(); setArrastando(true) }}
        onDragLeave={() => setArrastando(false)}
        onDrop={aoSoltar}
      >
        <input
          ref={inputRef}
          type="file"
          accept=".xlsx"
          hidden
          onChange={e => selecionar(e.target.files?.[0] ?? null)}
        />

        {arquivo ? (
          <div className="arquivo">
            <span className="icone">📄</span>
            <div className="info">
              <strong>{arquivo.name}</strong>
              <span>{formatarTamanho(arquivo.size)}</span>
            </div>
            <button
              className="remover"
              title="Remover arquivo"
              onClick={e => { e.stopPropagation(); limpar() }}
            >
              ✕
            </button>
          </div>
        ) : (
          <>
            <div className="icone-grande">⬆️</div>
            <p className="dz-titulo">Arraste a planilha aqui ou clique para selecionar</p>
            <p className="dz-hint">Apenas arquivos .xlsx</p>
          </>
        )}
      </div>

      <button className="primario importar-btn" disabled={!arquivo || enviando} onClick={enviar}>
        {enviando ? 'Importando...' : 'Importar'}
      </button>

      {resultado && (
        <div className="resultado">
          <div className="resumo">
            <span className="ok">✔ {resultado.importadas} importadas</span>
            <span className="falha">✖ {resultado.falhas} falhas</span>
            <span className="total">de {resultado.totalLinhas} linhas</span>
          </div>

          {resultado.erros.length > 0 && (
            <table>
              <thead>
                <tr><th>Linha</th><th>Coluna</th><th>Valor</th><th>Erro</th></tr>
              </thead>
              <tbody>
                {resultado.erros.map((e, i) => (
                  <tr key={i}>
                    <td>{e.linha}</td>
                    <td>{e.coluna}</td>
                    <td>{e.valor || '—'}</td>
                    <td>{e.erro}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </section>
  )
}
