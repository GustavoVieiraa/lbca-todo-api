import { useState, type FormEvent } from 'react'
import { api } from './api'

export function Login({ onLogin }: { onLogin: () => void }) {
  const [usuario, setUsuario] = useState('admin')
  const [senha, setSenha] = useState('admin123')
  const [erro, setErro] = useState<string | null>(null)
  const [carregando, setCarregando] = useState(false)

  async function entrar(e: FormEvent) {
    e.preventDefault()
    setErro(null)
    setCarregando(true)
    try {
      await api.login(usuario, senha)
      onLogin()
    } catch (ex) {
      setErro((ex as Error).message)
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="login">
      <form className="card" onSubmit={entrar}>
        <h1>📋 TodoApp</h1>
        <label>
          Usuário
          <input value={usuario} onChange={e => setUsuario(e.target.value)} autoFocus />
        </label>
        <label>
          Senha
          <input type="password" value={senha} onChange={e => setSenha(e.target.value)} />
        </label>
        {erro && <p className="erro">{erro}</p>}
        <button disabled={carregando}>{carregando ? 'Entrando...' : 'Entrar'}</button>
        <small>Credenciais padrão: <code>admin</code> / <code>admin123</code></small>
      </form>
    </div>
  )
}
