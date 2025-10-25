import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173
  },
  build: {
    // Enable code splitting and optimize chunks
    rollupOptions: {
      output: {
        manualChunks: {
          // Separate vendor libraries
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'ui-vendor': ['framer-motion', 'lucide-react'],
          'chart-vendor': ['apexcharts', 'react-apexcharts']
        },
        // Optimize chunk file names for caching
        chunkFileNames: (chunkInfo) => {
          const facadeModuleId = chunkInfo.facadeModuleId
          if (facadeModuleId) {
            // Create readable chunk names based on entry points
            const name = facadeModuleId.split('/').pop()?.replace('.tsx', '').replace('.ts', '')
            return `chunks/${name}-[hash].js`
          }
          return `chunks/[name]-[hash].js`
        },
        entryFileNames: 'entry/[name]-[hash].js',
        assetFileNames: 'assets/[name]-[hash].[ext]'
      }
    },
    // Optimize for production
    minify: true, // Use esbuild minification instead
    // Set chunk size warnings
    chunkSizeWarningLimit: 300, // Warn for chunks > 300kB
    // Enable source maps for debugging (optional)
    sourcemap: false // Set to true for debugging production issues
  },
  // Development optimizations
  optimizeDeps: {
    include: [
      'react',
      'react-dom',
      'react-router-dom',
      'framer-motion',
      'lucide-react',
      'apexcharts'
    ]
  }
})
