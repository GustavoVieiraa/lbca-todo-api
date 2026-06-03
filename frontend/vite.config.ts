import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// O proxy encaminha /api -> API (.NET) em http://localhost:8080,
// evitando CORS no desenvolvimento (o front e a API ficam na mesma origem).
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:8080',
        changeOrigin: true
      }
    }
  }
})
