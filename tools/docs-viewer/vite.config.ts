import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { readdirSync, readFileSync, statSync, cpSync, mkdirSync, rmSync } from 'node:fs'
import { join, relative, sep } from 'node:path'
import { fileURLToPath } from 'node:url'

const HERE = fileURLToPath(new URL('.', import.meta.url))
const DOCS_ROOT = join(HERE, '..', '..', 'docs')
const FRONTEND_PUBLIC_DEST = join(HERE, '..', '..', 'frontend', 'public', 'docs-viewer')

type Entry = { slug: string; rel: string }

/** Walk docs/ and return every markdown file as { slug, rel }. */
function walk(dir: string, base = DOCS_ROOT, out: Entry[] = []): Entry[] {
  for (const name of readdirSync(dir).sort()) {
    const full = join(dir, name)
    if (statSync(full).isDirectory()) walk(full, base, out)
    else if (name.toLowerCase().endsWith('.md')) {
      const rel = relative(base, full).split(sep).join('/')
      out.push({ slug: rel.replace(/\.md$/i, ''), rel })
    }
  }
  return out
}

/**
 * Reference docs live in docs/reference/<group>/<page>.md; their sidebar
 * section is the group. Everything else uses its top-level folder.
 */
function sectionOf(rel: string): string {
  const parts = rel.split('/')
  if (parts[0] === 'reference' && parts.length > 2) return parts[1]
  return parts[0]
}

function docsPlugin() {
  const ID = 'virtual:docs'
  const RESOLVED = '\0' + ID
  return {
    name: 'seldonvigil-docs',
    resolveId: (id: string) => (id === ID ? RESOLVED : null),
    load(id: string) {
      if (id !== RESOLVED) return null
      const docs = walk(DOCS_ROOT).map((f) => {
        const md = readFileSync(join(DOCS_ROOT, f.rel.split('/').join(sep)), 'utf8')
        const titleMatch = md.match(/^\s*#\s+(.+)$/m)
        const parts = f.slug.split('/')
        return {
          slug: f.slug,
          section: sectionOf(f.rel),
          title: titleMatch ? titleMatch[1].trim() : parts[parts.length - 1],
          markdown: md,
        }
      })
      return `export const DOCS = ${JSON.stringify(docs)};\n`
    },
  }
}

function copyToFrontendPlugin() {
  return {
    name: 'copy-to-frontend',
    closeBundle() {
      try {
        mkdirSync(FRONTEND_PUBLIC_DEST, { recursive: true })
        rmSync(FRONTEND_PUBLIC_DEST, { recursive: true, force: true })
        cpSync(join(HERE, 'dist'), FRONTEND_PUBLIC_DEST, { recursive: true, force: true })
      } catch (err) {
        console.warn('Could not copy docs-viewer dist to frontend/public:', err)
      }
    },
  }
}

export default defineConfig(({ command }) => ({
  // Built into the web image and served by nginx under /docs-viewer/, so
  // that /docs itself can be an in-app route that hosts the viewer in an iframe.
  base: command === 'build' ? '/docs-viewer/' : '/',
  plugins: [react(), docsPlugin(), copyToFrontendPlugin()],
  server: { host: '127.0.0.1', port: 4100, open: false },
  preview: { host: '0.0.0.0', port: 4100 },
}))
