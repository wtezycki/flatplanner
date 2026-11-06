import type { NeedResult } from '../api.ts'
import { VERDICT_PRESENTATION } from '../format.ts'

interface Props {
  need: string
  result: NeedResult
}

export default function NeedVerdict({ need, result }: Props) {
  const { icon, label } = VERDICT_PRESENTATION[result.verdict] ?? VERDICT_PRESENTATION.unknown
  return (
    <li className={`verdict verdict--${result.verdict}`}>
      <span className="verdict__icon" role="img" aria-label={label}>
        {icon}
      </span>
      <div>
        <p className="verdict__reason">{result.reason ?? need}</p>
        {result.evidence.map((evidence) => (
          <p key={`${evidence.kind}:${evidence.label}`} className="evidence">
            {evidence.kind === 'quote' ? `„${evidence.label}”` : evidence.label}
          </p>
        ))}
      </div>
    </li>
  )
}
