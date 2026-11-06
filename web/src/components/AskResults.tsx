import type { AskResponse } from '../api.ts'
import { ASK_GROUP_HEADINGS, formatListingsCount, pluralPl } from '../format.ts'
import AskCard from './AskCard.tsx'

interface Props {
  response: AskResponse
  onExcludeNeed: (index: number) => void
}

function Relaxations({ response, onExcludeNeed }: Props) {
  const { items, needs, relaxations } = response
  if (relaxations.length === 0) {
    return null
  }
  if (items.length === 0) {
    return (
      <ul className="ask-relaxations">
        {relaxations.map((relaxation) => (
          <li key={relaxation.need}>
            Bez warunku „{relaxation.need}”: {relaxation.count}{' '}
            {pluralPl(relaxation.count, 'mieszkanie', 'mieszkania', 'mieszkań')}
          </li>
        ))}
      </ul>
    )
  }
  const [top] = relaxations
  return (
    <div className="notice notice--info">
      <p>
        Żadne mieszkanie nie spełnia wszystkich warunków. Najczęściej odpada: {top.need} ({top.count} z {items.length}).
      </p>
      <ul className="ask-relaxations">
        {relaxations.map((relaxation) => (
          <li key={relaxation.need}>
            <span>{relaxation.need}</span>
            <button
              className="btn btn--secondary"
              type="button"
              onClick={() => onExcludeNeed(needs.indexOf(relaxation.need))}
            >
              Pokaż bez tego warunku ({relaxation.count})
            </button>
          </li>
        ))}
      </ul>
    </div>
  )
}

export default function AskResults({ response, onExcludeNeed }: Props) {
  const { summary, needs, declined, notices, items, unknownCount } = response
  const unknownItems = items.filter((item) => item.group === 'unknown')

  return (
    <div className="ask-results">
      <p className="ask-summary">
        <strong>Zrozumiałem:</strong> {summary}
      </p>
      {needs.length > 0 && (
        <ul className="ask-needs">
          {needs.map((need) => (
            <li key={need} className="ask-needs__chip">
              {need}
            </li>
          ))}
        </ul>
      )}
      {declined.map((entry) => (
        <div key={entry.text} className="notice notice--info">
          <span aria-hidden="true">ⓘ</span> „{entry.text}”: {entry.reason}
        </div>
      ))}
      {notices.map((notice) => (
        <div key={`${notice.code}:${notice.text}`} className="notice notice--warning">
          {notice.text}
        </div>
      ))}
      <p className="sr-only" aria-live="polite">
        {formatListingsCount(items.length)}
      </p>
      <Relaxations response={response} onExcludeNeed={onExcludeNeed} />
      {items.length === 0 && (
        <div className="empty">
          <p className="empty__title">Nie mamy mieszkań, które spełniają te warunki. Spróbuj opisać to inaczej.</p>
        </div>
      )}
      {Object.entries(ASK_GROUP_HEADINGS).map(([group, heading]) => {
        const groupItems = items.filter((item) => item.group === group)
        return groupItems.length === 0 ? null : (
          <section key={group} className="ask-group">
            {needs.length > 0 && <h2 className="ask-group__heading">{heading}</h2>}
            <ul className="ask-list">
              {groupItems.map((item) => (
                <li key={item.id}>
                  <AskCard item={item} needs={needs} />
                </li>
              ))}
            </ul>
          </section>
        )
      })}
      {unknownItems.length > 0 && (
        <details className="ask-group ask-unknown">
          <summary className="ask-group__heading">Ogłoszenie o tym nie mówi ({unknownCount})</summary>
          <ul className="ask-list">
            {unknownItems.map((item) => (
              <li key={item.id}>
                <AskCard item={item} needs={needs} />
              </li>
            ))}
          </ul>
        </details>
      )}
    </div>
  )
}
