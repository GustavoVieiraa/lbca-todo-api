import { createContext, useCallback, useContext, useMemo, useRef, useState, type ReactNode } from 'react'

type TipoToast = 'sucesso' | 'erro'

interface Toast {
  id: number
  tipo: TipoToast
  texto: string
}

interface ToastApi {
  sucesso: (texto: string) => void
  erro: (texto: string) => void
}

const ToastContext = createContext<ToastApi | null>(null)

export function useToast(): ToastApi {
  const ctx = useContext(ToastContext)
  if (!ctx) throw new Error('useToast deve ser usado dentro de <ToastProvider>.')
  return ctx
}

export function ToastProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const proximoId = useRef(1)

  const adicionar = useCallback((tipo: TipoToast, texto: string) => {
    const id = proximoId.current++
    setToasts(atuais => [...atuais, { id, tipo, texto }])
    setTimeout(() => setToasts(atuais => atuais.filter(t => t.id !== id)), 3800)
  }, [])

  const api = useMemo<ToastApi>(() => ({
    sucesso: texto => adicionar('sucesso', texto),
    erro: texto => adicionar('erro', texto)
  }), [adicionar])

  return (
    <ToastContext.Provider value={api}>
      {children}
      <div className="toaster">
        {toasts.map(t => (
          <div key={t.id} className={`toast ${t.tipo}`} role="alert">{t.texto}</div>
        ))}
      </div>
    </ToastContext.Provider>
  )
}
