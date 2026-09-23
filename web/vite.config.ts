import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      // Dev only. In production this is same-origin (see technical-plan.md §1:
      // API at /api/*, React build served from the same ASP.NET Core app).
      '/api': {
        target: 'http://localhost:5025',
        changeOrigin: true,
      },
    },
  },
})
