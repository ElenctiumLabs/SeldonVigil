import { useEffect, useRef } from 'react'
import { useTheme } from '@/hooks/useTheme'

interface CosmicBackdropCanvasProps {
  /** Star count, drift speed and FPS tuned for subtle ambient motion */
  stars?: number
  targetFps?: number
}

/**
 * CosmicBackdropCanvas — animated star-field backdrop ported from the
 * SeldonVigil identity. Theme-compliant: renders the deep
 * space gradient in dark mode and the warm albedo wash in light mode,
 * tracking the host application's theme in both directions.
 */
export default function CosmicBackdropCanvas({ stars: starCount = 50, targetFps = 12 }: CosmicBackdropCanvasProps) {
  const canvasRef = useRef<HTMLCanvasElement>(null)
  const { theme } = useTheme()
  const themeRef = useRef(theme)
  themeRef.current = theme
  // Lets the theme-sync effect re-render cached gradients without
  // tearing down the animation loop (keeps star positions stable)
  const refreshGradientsRef = useRef<() => void>(() => {})

  // Recolour the backdrop when the app theme changes (both directions)
  useEffect(() => {
    refreshGradientsRef.current()
  }, [theme])

  useEffect(() => {
    const canvas = canvasRef.current
    if (!canvas) return

    const ctx = canvas.getContext('2d')
    if (!ctx) return

    let animationFrameId: number
    let time = 0

    // Cache gradients to prevent allocating GPU textures 60x/sec
    let bgGrad: CanvasGradient | null = null
    let nebulaGrad1: CanvasGradient | null = null
    let nebulaGrad2: CanvasGradient | null = null

    const createGradients = (w: number, h: number) => {
      const light = themeRef.current === 'light'
      bgGrad = ctx.createRadialGradient(w / 2, h / 2, 50, w / 2, h / 2, Math.max(w, h))
      if (light) {
        bgGrad.addColorStop(0, '#ffffff')
        bgGrad.addColorStop(0.5, '#faf8f5')
        bgGrad.addColorStop(1, '#f2ece1')
      } else {
        bgGrad.addColorStop(0, '#0a0d18')
        bgGrad.addColorStop(0.5, '#080a0f')
        bgGrad.addColorStop(1, '#020306')
      }

      nebulaGrad1 = ctx.createRadialGradient(w * 0.3, h * 0.4, 0, w * 0.3, h * 0.4, 300)
      nebulaGrad1.addColorStop(0, light ? 'rgba(74, 144, 226, 0.04)' : 'rgba(74, 144, 226, 0.08)')
      nebulaGrad1.addColorStop(1, 'transparent')

      nebulaGrad2 = ctx.createRadialGradient(w * 0.7, h * 0.6, 0, w * 0.7, h * 0.6, 350)
      nebulaGrad2.addColorStop(0, light ? 'rgba(212, 175, 55, 0.05)' : 'rgba(212, 175, 55, 0.07)')
      nebulaGrad2.addColorStop(1, 'transparent')
    }

    const resize = () => {
      if (!canvas.parentElement) return
      canvas.width = canvas.parentElement.clientWidth
      canvas.height = canvas.parentElement.clientHeight
      createGradients(canvas.width, canvas.height)
    }

    window.addEventListener('resize', resize)
    resize()
    refreshGradientsRef.current = () => createGradients(canvas.width, canvas.height)

    // Create persistent cosmic stars — fewer for perf
    const stars = Array.from({ length: starCount }, () => ({
      x: Math.random() * 2000,
      y: Math.random() * 2000,
      size: Math.random() * 1.5 + 0.3,
      alpha: Math.random() * 0.6 + 0.1,
      hue: Math.random() > 0.5 ? 45 : 210, // Gold or Cyan
    }))

    let isVisible = true
    const handleVisibility = () => {
      const wasVisible = isVisible
      isVisible = !document.hidden
      if (isVisible && !wasVisible) {
        animationFrameId = requestAnimationFrame(render)
      }
    }
    document.addEventListener('visibilitychange', handleVisibility)

    // Throttle to configured FPS — stars twinkle slowly, no visual difference
    const FRAME_INTERVAL = 1000 / targetFps
    let lastRender = 0

    const render = (timestamp: number) => {
      if (!isVisible) return
      if (timestamp - lastRender < FRAME_INTERVAL) {
        animationFrameId = requestAnimationFrame(render)
        return
      }
      lastRender = timestamp
      time += 0.015
      const w = canvas.width
      const h = canvas.height

      if (bgGrad) {
        ctx.fillStyle = bgGrad
        ctx.fillRect(0, 0, w, h)
      }
      if (nebulaGrad1) {
        ctx.fillStyle = nebulaGrad1
        ctx.fillRect(0, 0, w, h)
      }
      if (nebulaGrad2) {
        ctx.fillStyle = nebulaGrad2
        ctx.fillRect(0, 0, w, h)
      }

      // Render cosmic star field — batch by hue to minimize fillStyle changes
      const goldStars: typeof stars = []
      const cyanStars: typeof stars = []
      for (const star of stars) {
        (star.hue === 45 ? goldStars : cyanStars).push(star)
      }

      const drawBatch = (batch: typeof stars, r: number, g: number, b: number) => {
        if (batch.length === 0) return
        ctx.fillStyle = `rgba(${r},${g},${b},0.5)`
        ctx.beginPath()
        for (const star of batch) {
          const screenX = (star.x + w / 2 + Math.sin(time * 0.1) * 10) % w
          const screenY = (star.y + h / 2 + Math.cos(time * 0.1) * 10) % h
          const finalX = screenX < 0 ? screenX + w : screenX
          const finalY = screenY < 0 ? screenY + h : screenY
          ctx.moveTo(finalX + star.size, finalY)
          ctx.arc(finalX, finalY, star.size, 0, Math.PI * 2)
        }
        ctx.globalAlpha = 0.6
        ctx.fill()
        ctx.globalAlpha = 1
      }

      drawBatch(goldStars, 255, 215, 0)
      drawBatch(cyanStars, 100, 200, 255)

      animationFrameId = requestAnimationFrame(render)
    }

    render(0)

    return () => {
      window.removeEventListener('resize', resize)
      document.removeEventListener('visibilitychange', handleVisibility)
      cancelAnimationFrame(animationFrameId)
    }
  }, [starCount, targetFps])

  return (
    <canvas
      ref={canvasRef}
      aria-hidden="true"
      className="absolute inset-0 pointer-events-none z-0"
      style={{ width: '100%', height: '100%' }}
    />
  )
}
