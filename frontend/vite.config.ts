import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
  ],
  server: {
    // The interface talks to the engine on the same origin, so no browser
    // permission is involved and the backend needs no cross origin rules.
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: false,
      },
    },
  },
  build: {
    // The engine serves the interface on its own address: one address, one
    // certificate, no rules between origins (Transport Security
    // Specification). The output is produced, never versioned.
    outDir: fileURLToPath(new URL('../backend/src/TivuStream.Pie.Api/wwwroot', import.meta.url)),
    emptyOutDir: true,
  },
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
})
