import {
  LayoutDashboard,
  Activity,
  ShieldAlert,
  DollarSign,
  Database,
  Scale,
  Link2,
  BrainCircuit,
  Users,
  Settings,
  BookOpen,
  LucideIcon,
} from 'lucide-react'

export interface NavItem {
  icon: LucideIcon
  label: string
  href: string
  badge?: string
}

export const navItems: NavItem[] = [
  { icon: LayoutDashboard, label: 'Dashboard',         href: '/' },
  { icon: Users,        label: 'Agents',            href: '/agents' },
  { icon: Activity,        label: 'Traces',            href: '/traces',   badge: 'Live' },
  { icon: ShieldAlert,     label: 'Security',          href: '/security' },
  { icon: DollarSign,      label: 'Cost', href: '/costs' },
  { icon: Database,        label: 'Broker',            href: '/broker' },
  { icon: Scale,           label: 'Governance',        href: '/governance' },
  { icon: Link2,           label: 'Integrations',      href: '/integrations' },
  { icon: BrainCircuit,    label: 'AI Reasoning',      href: '/ai-reasoning' },
  { icon: BookOpen,        label: 'Docs',              href: '/docs' },
  { icon: Settings,        label: 'Settings',          href: '/settings' },
]
