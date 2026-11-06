import type { ListingRisk } from '../api.ts'
import { RISK_KIND_LABELS, formatPrice } from '../format.ts'

interface Props {
  risks: ListingRisk[]
  totalWithExtrasPln?: number
}

export default function RiskList({ risks, totalWithExtrasPln }: Props) {
  if (risks.length === 0) {
    return null
  }
  return (
    <section className="risks">
      <h3 className="risks__heading">Na co uważać</h3>
      <ul className="risks__list">
        {risks.map((risk) => (
          <li key={`${risk.kind}:${risk.quote}`}>
            <p>
              <span className="badge badge--warning">{RISK_KIND_LABELS[risk.kind] ?? risk.kind}</span> {risk.reason}
            </p>
            <p className="evidence">„{risk.quote}”</p>
          </li>
        ))}
      </ul>
      {totalWithExtrasPln !== undefined && (
        <p className="risks__total num">Łącznie z dopłatą: {formatPrice(totalWithExtrasPln)}</p>
      )}
    </section>
  )
}
