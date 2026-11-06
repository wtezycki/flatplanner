import { useEffect, useState } from 'react'

const STEPS = ['Rozumiem pytanie', 'Czytam ogłoszenia', 'Sprawdzam dowody']
const STEP_START_MS = [0, 2000, 6000]

export default function AskProgress() {
  const [active, setActive] = useState(0)

  useEffect(() => {
    const timers = STEP_START_MS.map((delay, index) => window.setTimeout(() => setActive(index), delay))
    return () => timers.forEach((timer) => window.clearTimeout(timer))
  }, [])

  return (
    <p className="progress" role="status">
      <span>{STEPS[active]}</span>
      <span className="progress__dots" aria-hidden="true">
        <span />
        <span />
        <span />
      </span>
    </p>
  )
}
