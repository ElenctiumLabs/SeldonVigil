/**
 * HexagramMark — Solomon's Seal brand mark
 * SeldonVigil brand mark:
 * gold-filled circle at 80% opacity, crosshair/compass SVG cut-out
 * stroked in the background colour to create a punched-out silhouette.
 */
interface HexagramMarkProps {
  size?: number
  className?: string
}

export function SeldonVigilLogo({ size = 32, className }: HexagramMarkProps) {
  return (
    <div
      className={className}
      style={{
        position: 'relative',
        width: size,
        height: size,
        borderRadius: '50%',
        flexShrink: 0,
        display: 'inline-flex',
        alignItems: 'center',
        justifyContent: 'center',
      }}
    >
      {/* Gold filled circle */}
      <div
        aria-hidden="true"
        style={{
          position: 'absolute',
          inset: 0,
          borderRadius: '50%',
          background: '#fbbf24',
          opacity: 0.85,
        }}
      />
      {/* Compass / crosshair — stroked in background colour for cut-out effect */}
      <svg
        aria-hidden="true"
        style={{ position: 'relative', width: size, height: size }}
        viewBox="0 0 24 24"
        fill="none"
        stroke="#080a0f"
        strokeWidth="1.5"
        strokeLinecap="round"
      >
        <circle cx="12" cy="12" r="10" />
        <path d="M12 6v12M6 12h12" />
        <circle cx="12" cy="12" r="3" />
      </svg>
    </div>
  )
}

// Named alias so callers can use either name
export { SeldonVigilLogo as HexagramMark }
