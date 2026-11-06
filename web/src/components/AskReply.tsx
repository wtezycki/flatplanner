import type { AskResponse } from '../api.ts'
import { pluralPl } from '../format.ts'

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
    <div className="ask-relax">
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

export default function AskReply({ response, onExcludeNeed }: Props) {
  const { summary, needs, declined, notices } = response

  return (
    <div className="ask-reply">
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
        <p key={entry.text} className="ask-note">
          <span aria-hidden="true">ⓘ</span> „{entry.text}”: {entry.reason}
        </p>
      ))}
      {notices.map((notice) => (
        <p key={`${notice.code}:${notice.text}`} className="ask-note">
          <span aria-hidden="true">ⓘ</span> {notice.text}
        </p>
      ))}
      <Relaxations response={response} onExcludeNeed={onExcludeNeed} />
    </div>
  )
}
