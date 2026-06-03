import { useState } from 'react'
import { api } from './api'
import { useToast } from './toast'
import type { ImportacaoResultado } from './types'

export function ImportarView() {
  const toast = useToast()
  const [arquivo, setArquivo] = useState<File | null>(null)
  const [resultado, setResultado] = useState<ImportacaoResultado | null>(null)
  const [enviando, setEnviando] = useState(false)
  const [baixando, setBaixando] = useState<'completo' | 'erros' | null>(null)

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
        Linhas inválidas não impedem a importação — aparecem no relatório abaixo.
        <br />
        Não tem um arquivo? Baixe a planilha de exemplo (já com linhas válidas e inválidas para testar).
      </p>

      <div className="upload">
        <button onClick={() => baixarExemplo('completo')} disabled={baixando !== null}>
          {baixando === 'completo' ? 'Baixando...' : '⬇ Exemplo completo (10 tarefas)'}
        </button>
        <button onClick={() => baixarExemplo('erros')} disabled={baixando !== null}>
          {baixando === 'erros' ? 'Baixando...' : '⬇ Exemplo com erros'}
        </button>
      </div>

      <div className="upload">
        <input type="file" accept=".xlsx" onChange={e => setArquivo(e.target.files?.[0] ?? null)} />
        <button className="primario" disabled={!arquivo || enviando} onClick={enviar}>
          {enviando ? 'Enviando...' : 'Importar'}
        </button>
      </div>

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
