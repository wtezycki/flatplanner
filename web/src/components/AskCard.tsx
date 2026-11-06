import { Link } from 'react-router'
import type { AskItem } from '../api.ts'
import { formatArea, formatFloor, formatPrice, formatRooms } from '../format.ts'
import NeedVerdict from './NeedVerdict.tsx'
import RiskList from './RiskList.tsx'

interface Props {
  item: AskItem
  needs: string[]
}

export default function AskCard({ item, needs }: Props) {
  const parameters = [
    formatArea(item.areaM2),
    formatRooms(item.rooms),
    item.floor === undefined ? undefined : formatFloor(item.floor, item.buildingFloors, false),
  ].filter(Boolean)
  const location = item.districtName ? `${item.districtName}, ${item.cityName}` : item.cityName
  const verdicts = needs.map((need, index) => ({ need, result: item.needs[index] }))
  const failing = verdicts.filter(({ result }) => result.verdict === 'no')
  const ordered = item.group === 'near' ? [...failing, ...verdicts.filter((entry) => !failing.includes(entry))] : verdicts
  const hasAiColumn = verdicts.length > 0 || item.risks.length > 0

  return (
    <article className="ask-card">
      {item.imageUrl ? (
        <img className="ask-card__photo" src={item.imageUrl} alt="" loading="lazy" referrerPolicy="no-referrer" />
      ) : (
        <div className="ask-card__photo ask-card__photo--missing">Brak zdjęcia</div>
      )}
      <div className="ask-card__body">
        <p className="card__price num">{formatPrice(item.pricePln)}</p>
        <p className="card__parameters">{parameters.join(' · ')}</p>
        <p className="card__location">{location}</p>
        <p className="card__title">
          <Link className="card__link" to={`/listings/${item.id}`}>
            {item.title}
          </Link>
        </p>
        {item.aiSummary && <p className="ask-card__summary">AI: {item.aiSummary}</p>}
        {item.locationApproximate && (
          <p className="card__badges">
            <span className="badge">lokalizacja przybliżona, odległość orientacyjna</span>
          </p>
        )}
      </div>
      {hasAiColumn && (
        <div className="ask-card__ai">
          {verdicts.length > 0 && (
            <>
              <h3 className="ask-card__ai-heading">Dlaczego pasuje</h3>
              <ul className="verdicts">
                {ordered.map(({ need, result }) => (
                  <NeedVerdict key={need} need={need} result={result} />
                ))}
              </ul>
            </>
          )}
          <RiskList risks={item.risks} totalWithExtrasPln={item.totalWithExtrasPln} />
        </div>
      )}
    </article>
  )
}
