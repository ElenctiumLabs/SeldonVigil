/// <reference types="vite/client" />

declare module 'virtual:docs' {
  export const DOCS: Array<{
    slug: string
    section: string
    title: string
    markdown: string
  }>
}
