import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Em dev, /api é encaminhado para a API .NET (Nienow.Shinrikai.Api, porta 5080) — sem CORS.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
  build: {
    // `npm run build` grava no wwwroot do Nienow.Shinrikai.Web, que serve o SPA e encaminha /api para a API.
    outDir: '../wwwroot',
    emptyOutDir: true,
  },
})
