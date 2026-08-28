import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

// El backend .NET corre aparte. El proxy evita CORS en desarrollo y deja que el
// frontend hable siempre con rutas relativas /api/..., igual que hará en producción.
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // `host: true` ata el servidor a 0.0.0.0 en vez de a localhost, que es lo
    // que hace falta para abrirlo desde el teléfono en la misma red WiFi.
    host: true,
    port: 5173,
    proxy: {
      '/api': {
        target: process.env.PENUEL_API ?? 'http://localhost:5201',
        changeOrigin: true,
      },
    },
  },
})
