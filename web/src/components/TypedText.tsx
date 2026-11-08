import { useEffect, useState } from 'react'

const STEP_CHARACTERS = 3
const STEP_MS = 16

export default function TypedText({ text, animate }: { text: string; animate: boolean }) {
  const instant = !animate || window.matchMedia('(prefers-reduced-motion: reduce)').matches
  const [shown, setShown] = useState(instant ? text.length : 0)

  useEffect(() => {
    if (instant || shown >= text.length) {
      return
    }
    const timer = window.setTimeout(() => setShown(Math.min(text.length, shown + STEP_CHARACTERS)), STEP_MS)
    return () => window.clearTimeout(timer)
  }, [instant, shown, text])

  return <>{instant ? text : text.slice(0, shown)}</>
}
