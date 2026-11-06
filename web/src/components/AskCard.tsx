import { Link, useSearchParams } from 'react-router'
import type { AskItem } from '../api.ts'
import { formatArea, formatFloor, formatPrice, formatRooms, verdictPresentation } from '../format.ts'
import { listingSearch } from '../intent.ts'
import NeedVerdict from './NeedVerdict.tsx'
import RiskList from './RiskList.tsx'

interface Props {
  item: AskItem
  needs: string[]
  excludeNeeds: number[]
}

export default function AskCard({ item, needs, excludeNeeds }: Props) {
  const [searchParams] = useSearchParams()
  const parameters = [
    formatArea(item.areaM2),
    formatRooms(item.rooms),
    item.floor === undefined ? undefined : formatFloor(item.floor, item.buildingFloors, false),
  ].filter(Boolean)
  const location = item.districtName ? `${item.districtName}, ${item.cityName}` : item.cityName
  const verdicts = needs
    .map((need, index) => ({ need, index, result: item.needs[index] }))
    .filter(({ index }) => !excludeNeeds.includes(index))
  const failing = verdicts.filter(({ result }) => result.verdict === 'no')
  const ordered = item.group === 'near' ? [...failing, ...verdicts.filter((entry) => !failing.includes(entry))] : verdicts

  return (
    <article className="tile">
      <div className={item.imageUrl ? 'tile__media' : 'tile__media tile__media--missing'}>
        {item.imageUrl ? (
          <img src={item.imageUrl} alt="" loading="lazy" referrerPolicy="no-referrer" />
        ) : (
          'Brak zdjęcia'
        )}
      </div>
      <div className="tile__body">
        <p className="tile__price-line">
          <span className="card__price num">{formatPrice(item.pricePln)}</span>
          <span className="tile__parameters">{parameters.join(' · ')}</span>
        </p>
        <p className="card__location">
          {location}
          {item.locationApproximate && (
            <>
              {' '}
              <span className="badge">lokalizacja przybliżona, odległość orientacyjna</span>
            </>
          )}
        </p>
        <p className="card__title">
          <Link className="card__link" to={{ search: listingSearch(searchParams, item.id) }}>
            {item.title}
          </Link>
        </p>
        {verdicts.length > 0 && (
          <ul className="tile__pills">
            {ordered.map(({ need, result }) => {
              const { icon, label } = verdictPresentation(result.verdict)
              return (
                <li key={need} className={`verdict-pill verdict-pill--${result.verdict}`}>
                  <span role="img" aria-label={label}>
                    {icon}
                  </span>
                  {need}
                </li>
              )
            })}
          </ul>
        )}
        {item.aiSummary && (
          <div className="ai-box">
            <p className={verdicts.length > 0 ? 'ai-box__text ai-box__text--clamped' : 'ai-box__text'}>
              <span className="ai-box__label">
                <span aria-hidden="true">✨</span> AI:
              </span>{' '}
              {item.aiSummary}
            </p>
          </div>
        )}
        {verdicts.length > 0 && (
          <details className="tile__why">
            <summary>Dlaczego pasuje</summary>
            <ul className="verdicts">
              {ordered.map(({ need, result }) => (
                <NeedVerdict key={need} need={need} result={result} />
              ))}
            </ul>
          </details>
        )}
        <RiskList risks={item.risks} totalWithExtrasPln={item.totalWithExtrasPln} />
      </div>
    </article>
  )
}
