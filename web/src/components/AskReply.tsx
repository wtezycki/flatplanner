import { OFF_TOPIC_NOTICE } from '../api.ts'
import type { AskResponse } from '../api.ts'
import { pluralPl } from '../format.ts'
import TypedText from './TypedText.tsx'

interface RelaxationsProps {
  response: AskResponse
  onExcludeNeed: (index: number) => void
}

interface Props {
  reply: Pick<AskResponse, 'summary' | 'needs' | 'declined' | 'notices'>
  response?: AskResponse
  excludeNeeds: number[]
  animate: boolean
  onExcludeNeed?: (index: number) => void
  onRestoreNeeds?: () => void
}

function Relaxations({ response, onExcludeNeed }: RelaxationsProps) {
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

export default function AskReply({ reply, response, excludeNeeds, animate, onExcludeNeed, onRestoreNeeds }: Props) {
  const { summary, needs, declined, notices } = reply

  return (
    <div className="ask-reply">
      {summary.length > 0 && (
        <p className="ask-summary">
          <strong>Zrozumiałem:</strong> <TypedText text={summary} animate={animate} />
        </p>
      )}
      {needs.length > 0 && (
        <ul className="ask-needs">
          {needs.map((need, index) =>
            excludeNeeds.includes(index) ? null : (
              <li key={need} className="ask-needs__chip">
                {need}
                {onExcludeNeed && (
                  <button
                    className="ask-needs__remove"
                    type="button"
                    aria-label={`Usuń warunek „${need}”`}
                    onClick={() => onExcludeNeed(index)}
                  >
                    ×
                  </button>
                )}
              </li>
            ),
          )}
          {excludeNeeds.length > 0 && onRestoreNeeds && (
            <li>
              <button className="btn btn--link ask-needs__restore" type="button" onClick={onRestoreNeeds}>
                Przywróć wszystkie warunki
              </button>
            </li>
          )}
        </ul>
      )}
      {declined.map((entry) => (
        <p key={entry.text} className="ask-note">
          <span aria-hidden="true">ⓘ</span> „{entry.text}”: {entry.reason}
        </p>
      ))}
      {notices.map((notice) =>
        notice.code === OFF_TOPIC_NOTICE ? (
          <p key={notice.code} className="ask-offtopic">
            <TypedText text={notice.text} animate={animate} />
          </p>
        ) : (
          <p key={`${notice.code}:${notice.text}`} className="ask-note">
            <span aria-hidden="true">ⓘ</span> {notice.text}
          </p>
        ),
      )}
      {response && onExcludeNeed && <Relaxations response={response} onExcludeNeed={onExcludeNeed} />}
    </div>
  )
}
