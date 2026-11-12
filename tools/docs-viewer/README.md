# SeldonVigil Docs Viewer

A standalone Vite app that renders the repository's `docs/` folder as a
documentation site. It is built into the web image and served by nginx under
`/docs-viewer/`, and embedded in the app at the `/docs` route.

## Running locally

```bash
cd tools/docs-viewer
npm install
npm run dev      # http://127.0.0.1:4100
npm run build    # outputs dist/ (base path /docs/)
```

## How it works

- A Vite plugin walks `docs/` at build time and inlines every markdown file into
  the bundle as a virtual module (`virtual:docs`). Nothing is fetched at
  runtime, so the page works without a backend.
- Documents are grouped into sidebar sections by folder. Documents under
  `docs/reference/<group>/` use `<group>` as their section; everything else uses
  its top-level folder.
- Diagrams use Mermaid fenced blocks (```mermaid). Mermaid is lazy-loaded only
  when a diagram is rendered, so it is not part of the initial bundle.
- The theme follows the main app's `seldonvigil-theme` preference and falls back to
  the OS setting.

## Updating the documentation

1. Edit the markdown under `docs/`. The reference documentation lives in
   `docs/reference/`, grouped by section (overview, stack, services, modules,
   api, frontend, data, deployment, decisions).
2. Rebuild the web image so the docs are re-inlined:
   `docker compose -f infrastructure/docker/docker-compose.yml up -d --build web`
3. The site at `/docs` reflects the change.

Because the site is generated from the docs folder, the published documentation
always matches the repository.
