import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// Derleme ciktisi API'nin wwwroot klasorune gider: tek dagitilabilir uygulama (ADR-001 3.2).
export default defineConfig({
  plugins: [react()],
  build: {
    outDir: '../UUSport.Api/wwwroot',
    emptyOutDir: true,
    sourcemap: false,
  },
  server: {
    port: 5173,
    proxy: { '/api': 'http://127.0.0.1:5080' },
  },
})
