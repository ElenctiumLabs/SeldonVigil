import { motion } from 'framer-motion'

interface FadeInProps {
  children: React.ReactNode
  delay?: number
  duration?: number
  className?: string
}

/**
 * Pure opacity fade — no vertical/horizontal translation, so content
 * never shifts position while appearing.
 */
export function FadeIn({
  children,
  delay = 0,
  duration = 0.5,
  className = '',
}: FadeInProps) {
  return (
    <motion.div
      initial={{ opacity: 0 }}
      animate={{ opacity: 1 }}
      transition={{ duration, delay, ease: 'easeOut' }}
      className={className}
    >
      {children}
    </motion.div>
  )
}
