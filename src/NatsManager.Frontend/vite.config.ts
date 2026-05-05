import { createElement } from 'react'
import { renderToStaticMarkup } from 'react-dom/server'
import { ColorSchemeScript } from '@mantine/core'
import { defineConfig, type Plugin } from 'vite'
import react from '@vitejs/plugin-react'

const backendTarget =
  process.env.services__backend__http__0 || process.env.services__backend__https__0 || 'http://localhost:5062'

function mantineColorSchemePlugin(): Plugin {
  const scriptHtml = renderToStaticMarkup(
    createElement(ColorSchemeScript, { defaultColorScheme: 'auto' }),
  )
  return {
    name: 'mantine-color-scheme-script',
    transformIndexHtml(html) {
      return html.replace('<head>', `<head>${scriptHtml}`)
    },
  }
}

export default defineConfig({
  plugins: [react(), mantineColorSchemePlugin()],
  server: {
    port: parseInt(process.env.PORT || '5173'),
    proxy: {
      '/api': {
        target: backendTarget,
        changeOrigin: true,
        secure: false,
      },
      '/hubs': {
        target: backendTarget,
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
  build: {
    outDir: 'dist',
  },
})
