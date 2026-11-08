import type { ListingRisk } from '../api.ts'
import { PRICE_OUTLIER_HINT, RISK_KIND_HINTS, RISK_KIND_LABELS, RISK_SOURCE_NOTE } from '../format.ts'
import Icon from './Icon.tsx'

interface Props {
  risks: ListingRisk[]
  priceOutlier: boolean
}

export default function RiskBox({ risks, priceOutlier }: Props) {
  if (risks.length === 0 && !priceOutlier) {
    return null
  }
  return (
    <section className="risk-box" aria-labelledby="risk-box-title">
      <h2 id="risk-box-title" className="risk-box__title">
        <Icon name="risk" />
        Na co uważać
      </h2>
      <ul className="risk-box__list">
        {priceOutlier && (
          <li>
            <p className="risk-box__kind">Nietypowa cena</p>
            <p>{PRICE_OUTLIER_HINT}</p>
          </li>
        )}
        {risks.map((risk) => (
          <li key={`${risk.kind}:${risk.quote}`}>
            <p className="risk-box__kind" title={RISK_KIND_HINTS[risk.kind]}>
              {RISK_KIND_LABELS[risk.kind] ?? risk.kind}
            </p>
            <p>{risk.reason}</p>
            <blockquote className="risk-box__quote">„{risk.quote}”</blockquote>
          </li>
        ))}
      </ul>
      {risks.length > 0 && <p className="risk-box__note">{RISK_SOURCE_NOTE}</p>}
    </section>
  )
}
