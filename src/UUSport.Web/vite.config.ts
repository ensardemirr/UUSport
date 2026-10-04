import react from '@vitejs/plugin-react'
import { defineConfig, type Plugin } from 'vite'

// Her derlemenin kimligi. Sunucu bunu wwwroot/version.json'dan okuyup API yanitlarina ekler;
// acik sekmedeki kimlik farkliysa arayuz "yeni surum" bandi gosterir (S-08).
const buildId = new Date().toISOString()

function versionFile(): Plugin {
  return {
    name: 'uusport-version-file',
    apply: 'build',
    generateBundle() {
      this.emitFile({ type: 'asset', fileName: 'version.json', source: JSON.stringify({ build: buildId }) })
    },
  }
}

// Derleme ciktisi API'nin wwwroot klasorune gider: tek dagitilabilir uygulama (ADR-001 3.2).
export default defineConfig({
  plugins: [react(), versionFile()],
  define: { __APP_BUILD__: JSON.stringify(buildId) },
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
