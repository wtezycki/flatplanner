import { useSearchParams } from 'react-router'
import type { AskItem } from '../api.ts'
import { ASK_WHY_LABELS, formatPrice, verdictPresentation } from '../format.ts'
import { listingSearch } from '../intent.ts'
import type { CardLabels } from '../useCardLabels.ts'
import Icon from './Icon.tsx'
import ListingCardBody from './ListingCardBody.tsx'
import NeedVerdict from './NeedVerdict.tsx'
import PhotoCarousel from './PhotoCarousel.tsx'

interface Props {
  item: AskItem
  labels: CardLabels
  needs: string[]
  excludeNeeds: number[]
}

export default function AskCard({ item, labels, needs, excludeNeeds }: Props) {
  const [searchParams] = useSearchParams()
  const { listing } = item
  const href = { search: listingSearch(searchParams, listing.id) }
  const verdicts = needs
    .map((need, index) => ({ need, index, result: item.needs[index] }))
    .filter(({ index }) => !excludeNeeds.includes(index))
  const failing = verdicts.filter(({ result }) => result.verdict === 'no')
  const ordered = item.group === 'near' ? [...failing, ...verdicts.filter((entry) => !failing.includes(entry))] : verdicts
  const explained = ordered.filter(({ result }) => result.reason)

  return (
    <article className="tile">
      <PhotoCarousel urls={listing.imageUrls} href={href} />
      <ListingCardBody
        item={listing}
        labels={labels}
        href={href}
        priceNote={item.totalWithExtrasPln === undefined ? undefined : `Łącznie z dopłatą: ${formatPrice(item.totalWithExtrasPln)}`}
      >
        {verdicts.length > 0 && (
          <div className="tile__needs">
            <ul className="tile__pills">
              {ordered.map(({ need, result }) => {
                const { icon, label } = verdictPresentation(result.verdict)
                return (
                  <li key={need} className={`verdict-pill verdict-pill--${result.verdict}`}>
                    <span role="img" aria-label={label}>
                      <Icon name={icon} />
                    </span>
                    <span className="verdict-pill__text" title={need}>
                      {need}
                    </span>
                  </li>
                )
              })}
            </ul>
            {explained.length > 0 && (
              <details className="tile__why">
                <summary>{ASK_WHY_LABELS[item.group] ?? ASK_WHY_LABELS.full}</summary>
                <ul className="verdicts">
                  {explained.map(({ need, result }) => (
                    <NeedVerdict key={need} need={need} result={result} />
                  ))}
                </ul>
              </details>
            )}
          </div>
        )}
      </ListingCardBody>
    </article>
  )
}
