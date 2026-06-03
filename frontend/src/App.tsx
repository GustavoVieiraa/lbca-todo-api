import { useState } from 'react'
import { auth } from './api'
import { Login } from './Login'
import { TarefasView } from './TarefasView'
import { ImportarView } from './ImportarView'

type Aba = 'tarefas' | 'importar'

export function App() {
  const [autenticado, setAutenticado] = useState(auth.autenticado)
  const [aba, setAba] = useState<Aba>('tarefas')

  if (!autenticado) {
    return <Login onLogin={() => setAutenticado(true)} />
  }

  return (
    <div className="container">
      <header>
        <h1>📋 TodoApp</h1>
        <nav>
          <button className={aba === 'tarefas' ? 'ativo' : ''} onClick={() => setAba('tarefas')}>
            Tarefas
          </button>
          <button className={aba === 'importar' ? 'ativo' : ''} onClick={() => setAba('importar')}>
            Importar planilha
          </button>
          <button className="sair" onClick={() => { auth.clear(); setAutenticado(false) }}>
            Sair
          </button>
        </nav>
      </header>

      {aba === 'tarefas' ? <TarefasView /> : <ImportarView />}
    </div>
  )
}
