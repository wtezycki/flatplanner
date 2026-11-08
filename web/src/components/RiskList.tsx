import type { ListingRisk } from '../api.ts'
import { RISK_KIND_HINTS, RISK_KIND_LABELS, formatPrice } from '../format.ts'

interface Props {
  risks: ListingRisk[]
  totalWithExtrasPln?: number
}

export default function RiskList({ risks, totalWithExtrasPln }: Props) {
  if (risks.length === 0) {
    return null
  }
  return (
    <div className="risks">
      <details>
        <summary>
          Na co uważać ({risks.length})
          {risks.map((risk) => (
            <span key={`${risk.kind}:${risk.quote}`} className="badge badge--warning" title={RISK_KIND_HINTS[risk.kind]}>
              {RISK_KIND_LABELS[risk.kind] ?? risk.kind}
            </span>
          ))}
        </summary>
        <ul className="risks__list">
          {risks.map((risk) => (
            <li key={`${risk.kind}:${risk.quote}`}>
              <p>{risk.reason}</p>
              <p className="chips">
                <span className="chip" title={risk.quote}>
                  „{risk.quote}”
                </span>
              </p>
            </li>
          ))}
        </ul>
      </details>
      {totalWithExtrasPln !== undefined && <p className="risks__total num">Łącznie z dopłatą: {formatPrice(totalWithExtrasPln)}</p>}
    </div>
  )
}
