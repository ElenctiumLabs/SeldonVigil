import { motion } from 'framer-motion'

interface SlideInProps {
  children: React.ReactNode
  delay?: number
  duration?: number
  className?: string
  direction?: 'left' | 'right'
}

export function SlideIn({ 
  children, 
  delay = 0, 
  duration = 0.3, 
  className = '',
  direction = 'left'
}: SlideInProps) {
  const initialX = direction === 'left' ? -20 : 20

  return (
    <motion.div
      initial={{ opacity: 0, x: initialX }}
      animate={{ opacity: 1, x: 0 }}
      transition={{ duration, delay, ease: 'easeOut' }}
      className={className}
    >
      {children}
    </motion.div>
  )
}