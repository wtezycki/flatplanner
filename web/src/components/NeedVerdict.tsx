import type { NeedResult } from '../api.ts'
import { verdictPresentation } from '../format.ts'
import Icon from './Icon.tsx'

interface Props {
  need: string
  result: NeedResult
}

export default function NeedVerdict({ need, result }: Props) {
  const { icon, label } = verdictPresentation(result.verdict)
  return (
    <li className={`verdict verdict--${result.verdict}`}>
      <span className="verdict__icon" role="img" aria-label={label}>
        <Icon name={icon} />
      </span>
      <div className="verdict__text">
        <p className="verdict__reason">{result.reason || need}</p>
        {result.evidence.length > 0 && (
          <p className="chips">
            {result.evidence.slice(0, 1).map((evidence) => (
              <span key={`${evidence.kind}:${evidence.label}`} className="chip" title={evidence.label}>
                {evidence.kind === 'quote' ? `„${evidence.label}”` : evidence.label}
              </span>
            ))}
          </p>
        )}
      </div>
    </li>
  )
}
