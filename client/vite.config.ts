import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    port: 5173,
    proxy: {
      '/hubs': {
        target: 'http://localhost:5079',
        ws: true,
        changeOrigin: true
      },
      '/health': {
        target: 'http://localhost:5079',
        changeOrigin: true
      }
    }
  },
  build: {
    outDir: 'dist',
    emptyOutDir: true,
    sourcemap: true,
    chunkSizeWarningLimit: 2500
  }
});
