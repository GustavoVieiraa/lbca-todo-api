import React from 'react'
import ReactDOM from 'react-dom/client'
import { App } from './App'
import { ToastProvider } from './toast'
import favicon from './assets/img-lbca.png'
import './styles.css'

// Favicon a partir do asset da LBCA.
const iconLink = document.querySelector<HTMLLinkElement>("link[rel~='icon']") ?? document.createElement('link')
iconLink.rel = 'icon'
iconLink.href = favicon
document.head.appendChild(iconLink)

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <ToastProvider>
      <App />
    </ToastProvider>
  </React.StrictMode>
)
