import { useLayout } from '@/hooks/useLayout'
import { DefaultLayout } from './DefaultLayout'
import { CommandCenterLayout } from './layouts/CommandCenter'
import { FloatingOverlayLayout } from './layouts/FloatingOverlay'
import { EditorialLayout } from './layouts/Editorial'

export function LayoutShell() {
  const { layout } = useLayout()

  switch (layout) {
    case 'command-center':
      return <CommandCenterLayout />
    case 'floating-overlay':
      return <FloatingOverlayLayout />
    case 'editorial':
      return <EditorialLayout />
    case 'default':
    default:
      return <DefaultLayout />
  }
}
