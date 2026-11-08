import type { AskResponse } from '../api.ts'
import { ASK_GROUP_HEADINGS, formatListingsCount } from '../format.ts'
import { useCardLabels } from '../useCardLabels.ts'
import AskCard from './AskCard.tsx'
import AskProgress from './AskProgress.tsx'
import Icon from './Icon.tsx'
import type { IconName } from './Icon.tsx'
import Pagination from './Pagination.tsx'

const GROUP_ICONS: Record<string, IconName> = { full: 'check', partial: 'halfCircle', near: 'risk' }

const ASK_PAGE_SIZE = 12

interface Props {
  response: AskResponse
  excludeNeeds: number[]
  page: number
  interactive: boolean
  checkingMore: boolean
  onCheckMore: () => void
}

export default function AskResults({ response, excludeNeeds, page, interactive, checkingMore, onCheckMore }: Props) {
  const { needs, items, unknownCount, remainingCount } = response
  const cardLabels = useCardLabels()
  const matching = items.filter((item) => item.group !== 'unknown')
  const unknownItems = items.filter((item) => item.group === 'unknown')
  const totalPages = Math.max(1, Math.ceil(matching.length / ASK_PAGE_SIZE))
  const current = Math.min(Math.max(page, 1), totalPages)
  const pageItems = matching.slice((current - 1) * ASK_PAGE_SIZE, current * ASK_PAGE_SIZE)

  if (items.length === 0) {
    return (
      <div className="empty">
        <p className="empty__title">Nie mamy mieszkań, które spełniają te warunki. Spróbuj opisać to inaczej.</p>
      </div>
    )
  }

  return (
    <div className="ask-results">
      <p className="panel__count" aria-live="polite">
        {formatListingsCount(items.length)}
      </p>
      {Object.entries(ASK_GROUP_HEADINGS).map(([group, heading]) => {
        const groupItems = pageItems.filter((item) => item.group === group)
        return groupItems.length === 0 ? null : (
          <section key={group} className="ask-group">
            {needs.length > 0 && (
              <h2 className={`ask-group__heading ask-group__heading--${group}`}>
                <Icon name={GROUP_ICONS[group]} />
                {heading}
                <span className="ask-group__count num">{matching.filter((item) => item.group === group).length}</span>
              </h2>
            )}
            <ul className="ask-list">
              {groupItems.map((item) => (
                <li key={item.listing.id}>
                  <AskCard item={item} labels={cardLabels(item.listing)} needs={needs} excludeNeeds={excludeNeeds} />
                </li>
              ))}
            </ul>
          </section>
        )
      })}
      {interactive && <Pagination page={current} totalPages={totalPages} />}
      {interactive && current === totalPages && unknownItems.length > 0 && (
        <details className="ask-group ask-unknown">
          <summary className="ask-group__heading">Ogłoszenie o tym nie mówi ({unknownCount})</summary>
          <ul className="ask-list">
            {unknownItems.map((item) => (
              <li key={item.listing.id}>
                <AskCard item={item} labels={cardLabels(item.listing)} needs={needs} excludeNeeds={excludeNeeds} />
              </li>
            ))}
          </ul>
        </details>
      )}
      {interactive && current === totalPages && remainingCount > 0 && (
        <div className="ask-more">
          <p className="ask-more__line">
            Sprawdzone: {items.length} z {items.length + remainingCount} pasujących ogłoszeń, od najbardziej obiecujących.
          </p>
          {checkingMore ? (
            <AskProgress stage="reading" />
          ) : (
            <button className="btn btn--secondary" type="button" onClick={onCheckMore}>
              Sprawdź kolejne ogłoszenia
            </button>
          )}
        </div>
      )}
    </div>
  )
}
