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
    <div className="progress" role="status">
      <ol className="progress__steps">
        {STEPS.map((step, index) => (
          <li
            key={step}
            className={index <= active ? 'progress__step progress__step--lit' : 'progress__step'}
            aria-current={index === active ? 'step' : undefined}
          >
            <span aria-hidden="true">{index < active ? '✓' : index === active ? '…' : '○'}</span> {step}
          </li>
        ))}
      </ol>
    </div>
  )
}
