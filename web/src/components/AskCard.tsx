import { Link } from 'react-router'
import type { AskItem } from '../api.ts'
import { formatArea, formatFloor, formatPrice, formatRooms } from '../format.ts'
import NeedVerdict from './NeedVerdict.tsx'
import RiskList from './RiskList.tsx'

interface Props {
  item: AskItem
  needs: string[]
}

const SENTENCE_END = /(?<=[.!?])\s+/

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
          <Link className="card__link" to={`/listings/${item.id}`}>
            {item.title}
          </Link>
        </p>
        {item.aiSummary && (
          <div className="ai-box">
            <p className="ai-box__label">
              <span aria-hidden="true">✨</span> AI:
            </p>
            {item.aiSummary.split(SENTENCE_END).map((sentence) => (
              <p key={sentence}>{sentence}</p>
            ))}
          </div>
        )}
        {verdicts.length > 0 && (
          <div className="tile__verdicts">
            <h3 className="tile__label">Dlaczego pasuje</h3>
            <ul className="verdicts">
              {ordered.map(({ need, result }) => (
                <NeedVerdict key={need} need={need} result={result} />
              ))}
            </ul>
          </div>
        )}
        <RiskList risks={item.risks} totalWithExtrasPln={item.totalWithExtrasPln} />
      </div>
    </article>
  )
}
