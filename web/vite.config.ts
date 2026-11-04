import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// Em dev, /api é encaminhado para a API .NET (Nienow.Shinrikai, porta 5080) — sem CORS.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5080',
    },
  },
  build: {
    // `npm run build` publica dentro da API, que serve o SPA (UseStaticFiles + fallback).
    outDir: '../Nienow.Shinrikai/wwwroot',
    emptyOutDir: true,
  },
})
