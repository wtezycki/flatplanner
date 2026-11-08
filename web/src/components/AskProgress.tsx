const LABELS = {
  understanding: 'Rozumiem pytanie',
  reading: 'Czytam ogłoszenia i sprawdzam dowody',
}

export default function AskProgress({ stage }: { stage: keyof typeof LABELS }) {
  return (
    <p className="progress" role="status">
      <span>{LABELS[stage]}</span>
      <span className="progress__dots" aria-hidden="true">
        <span />
        <span />
        <span />
      </span>
    </p>
  )
}
