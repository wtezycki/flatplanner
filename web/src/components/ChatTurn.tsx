import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { ApiError, DEFAULT_RETRY_AFTER_SECONDS, streamAsk } from '../api.ts'
import { useAskTurn } from '../useAskTurn.ts'
import AskProgress from './AskProgress.tsx'
import AskReply from './AskReply.tsx'
import AskResults from './AskResults.tsx'
import Icon from './Icon.tsx'

interface Props {
  history: string[]
  message: string
  isLast: boolean
  page: number
}

function errorMessage(error: Error): string {
  if (error instanceof ApiError && error.status === 429) {
    return `Za dużo zapytań. Spróbuj ponownie za ${error.retryAfter ?? DEFAULT_RETRY_AFTER_SECONDS} s.`
  }
  return 'Nie udało się połączyć. Spróbuj ponownie lub skorzystaj z klasycznych filtrów.'
}

export default function ChatTurn({ history, message, isLast, page }: Props) {
  const [excludeNeeds, setExcludeNeeds] = useState<number[]>([])
  const { query, queryKey, understood } = useAskTurn(history, message, excludeNeeds)
  const queryClient = useQueryClient()
  const response = query.data
  const reply = response ?? understood

  const more = useMutation({
    mutationFn: () => streamAsk({ message, history, excludeNeeds, more: true }, undefined),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ['ask', history, message], refetchType: 'none' })
      queryClient.setQueryData(queryKey, data)
    },
  })

  const results = response && (
    <AskResults
      response={response}
      excludeNeeds={excludeNeeds}
      page={isLast ? page : 1}
      interactive={isLast}
      checkingMore={more.isPending}
      onCheckMore={() => more.mutate()}
    />
  )

  return (
    <section className="turn" aria-busy={query.isFetching}>
      <div className="turn__user">
        <p className="bubble">
          <span className="sr-only">Twoje pytanie: </span>
          {message}
        </p>
      </div>
      <div className="turn__answer">
        <span className="turn__avatar" aria-hidden="true">
          <Icon name="sparkles" />
        </span>
        <div className="turn__content">
          <span className="sr-only">Odpowiedź: </span>
          {query.isError && (
            <div className="notice notice--error" role="alert">
              <p>{errorMessage(query.error)}</p>
              <button className="btn btn--secondary" type="button" onClick={() => void query.refetch()}>
                Spróbuj ponownie
              </button>
            </div>
          )}
          {!reply && query.isPending && <AskProgress stage="understanding" />}
          {reply && (
            <AskReply
              reply={reply}
              response={response}
              excludeNeeds={excludeNeeds}
              animate={understood !== undefined && isLast}
              onExcludeNeed={isLast ? (index) => setExcludeNeeds([...excludeNeeds, index]) : undefined}
              onRestoreNeeds={isLast ? () => setExcludeNeeds([]) : undefined}
            />
          )}
          {reply && !response && query.isPending && (
            <>
              <AskProgress stage="reading" />
              {isLast && (
                <ul className="ask-list" aria-hidden="true">
                  {[1, 2, 3].map((placeholder) => (
                    <li key={placeholder}>
                      <div className="tile tile--skeleton" />
                    </li>
                  ))}
                </ul>
              )}
            </>
          )}
          {response && (
            <div className={query.isPlaceholderData ? 'ask__answer cards--stale' : 'ask__answer'}>
              {isLast ? (
                results
              ) : (
                <details className="turn__older">
                  <summary>Pokaż wyniki ({response.items.length})</summary>
                  {results}
                </details>
              )}
              {more.isError && (
                <div className="notice notice--error" role="alert">
                  {errorMessage(more.error)}
                </div>
              )}
            </div>
          )}
        </div>
      </div>
    </section>
  )
}
