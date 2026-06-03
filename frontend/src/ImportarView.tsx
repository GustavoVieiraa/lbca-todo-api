import { useState } from 'react'
import { api } from './api'
import type { ImportacaoResultado } from './types'

export function ImportarView() {
  const [arquivo, setArquivo] = useState<File | null>(null)
  const [resultado, setResultado] = useState<ImportacaoResultado | null>(null)
  const [erro, setErro] = useState<string | null>(null)
  const [carregando, setCarregando] = useState(false)

  async function enviar() {
    if (!arquivo) return
    setErro(null)
    setResultado(null)
    setCarregando(true)
    try {
      setResultado(await api.importar(arquivo))
    } catch (ex) {
      setErro((ex as Error).message)
    } finally {
      setCarregando(false)
    }
  }

  return (
    <section>
      <h2>Importar tarefas (.xlsx)</h2>
      <p className="ajuda">
        Colunas esperadas: <strong>Título</strong>, <strong>Descrição</strong>,
        {' '}<strong>Data de Vencimento</strong>, <strong>Prioridade</strong>.
        Linhas inválidas não impedem a importação — aparecem no relatório abaixo.
      </p>

      <div className="upload">
        <input type="file" accept=".xlsx" onChange={e => setArquivo(e.target.files?.[0] ?? null)} />
        <button disabled={!arquivo || carregando} onClick={enviar}>
          {carregando ? 'Enviando...' : 'Importar'}
        </button>
      </div>

      {erro && <p className="erro">{erro}</p>}

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
