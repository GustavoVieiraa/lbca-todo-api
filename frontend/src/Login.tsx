import { useState, type FormEvent } from 'react'
import { api } from './api'
import { useToast } from './toast'
import logo from './assets/logo-lbca.png'

export function Login({ onLogin }: { onLogin: () => void }) {
  const toast = useToast()
  const [usuario, setUsuario] = useState('admin')
  const [senha, setSenha] = useState('admin123')
  const [carregando, setCarregando] = useState(false)

  async function entrar(e: FormEvent) {
    e.preventDefault()
    setCarregando(true)
    try {
      await api.login(usuario, senha)
      onLogin()
    } catch (ex) {
      toast.erro((ex as Error).message)
    } finally {
      setCarregando(false)
    }
  }

  return (
    <div className="login">
      <form className="card" onSubmit={entrar}>
        <img className="logo" src={logo} alt="LBCA — Lee Brock Camargo Advogados" />
        <label>
          Usuário
          <input value={usuario} onChange={e => setUsuario(e.target.value)} autoFocus />
        </label>
        <label>
          Senha
          <input type="password" value={senha} onChange={e => setSenha(e.target.value)} />
        </label>
        <button className="primario" disabled={carregando}>
          {carregando ? 'Entrando...' : 'Entrar'}
        </button>
        <small>Credenciais padrão: <code>admin</code> / <code>admin123</code></small>
      </form>
    </div>
  )
}
