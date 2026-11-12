import { motion, useSpring } from 'framer-motion'
import { useEffect, useState } from 'react'

interface CountUpProps {
  value: number
  className?: string
  decimals?: number
}

export function CountUp({ 
  value, 
  className = '',
  decimals = 0
}: CountUpProps) {
  const [currentValue, setCurrentValue] = useState(0)
  
  const spring = useSpring(0, {
    stiffness: 50,
    damping: 25,
  })

  useEffect(() => {
    spring.set(value)
    const unsubscribe = spring.on('change', (latest) => {
      setCurrentValue(latest)
    })
    return unsubscribe
  }, [value, spring])

  return (
    <motion.span className={className}>
      {currentValue.toFixed(decimals)}
    </motion.span>
  )
}