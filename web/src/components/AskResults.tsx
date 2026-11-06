import type { AskResponse } from '../api.ts'
import { ASK_GROUP_HEADINGS, formatListingsCount } from '../format.ts'
import AskCard from './AskCard.tsx'
import Pagination from './Pagination.tsx'

const ASK_PAGE_SIZE = 12

interface Props {
  response: AskResponse
  excludeNeeds: number[]
  page: number
}

export default function AskResults({ response, excludeNeeds, page }: Props) {
  const { needs, items, unknownCount } = response
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
            {needs.length > 0 && <h2 className="ask-group__heading">{heading}</h2>}
            <ul className="ask-list">
              {groupItems.map((item) => (
                <li key={item.id}>
                  <AskCard item={item} needs={needs} excludeNeeds={excludeNeeds} />
                </li>
              ))}
            </ul>
          </section>
        )
      })}
      <Pagination page={current} totalPages={totalPages} />
      {current === totalPages && unknownItems.length > 0 && (
        <details className="ask-group ask-unknown">
          <summary className="ask-group__heading">Ogłoszenie o tym nie mówi ({unknownCount})</summary>
          <ul className="ask-list">
            {unknownItems.map((item) => (
              <li key={item.id}>
                <AskCard item={item} needs={needs} excludeNeeds={excludeNeeds} />
              </li>
            ))}
          </ul>
        </details>
      )}
    </div>
  )
}
