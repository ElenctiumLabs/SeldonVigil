import React from 'react'
import ReactDOM from 'react-dom/client'
import App from './App.tsx'
import './index.css'
import { ensureDevToken } from '@/lib/devAuth'

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <App />
  </React.StrictMode>,
)

// Attempt to fetch and store a dev token when in development
if (import.meta.env.DEV) {
  ensureDevToken();
}
