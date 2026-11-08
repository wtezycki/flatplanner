import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { useSearchParams } from 'react-router'
import { ApiError, DEFAULT_RETRY_AFTER_SECONDS, metaQuery, postAsk } from '../api.ts'
import AskProgress from '../components/AskProgress.tsx'
import AskReply from '../components/AskReply.tsx'
import AskResults from '../components/AskResults.tsx'
import ListingDetailPage from './ListingDetailPage.tsx'

function askErrorMessage(error: Error, maxLength: number | undefined): string {
  if (error instanceof ApiError && error.status === 429) {
    return `Za dużo zapytań. Spróbuj ponownie za ${error.retryAfter ?? DEFAULT_RETRY_AFTER_SECONDS} s.`
  }
  if (error instanceof ApiError && error.status === 400 && maxLength !== undefined) {
    return `Wpisz od 1 do ${maxLength} znaków.`
  }
  return 'Nie udało się połączyć. Spróbuj ponownie lub skorzystaj z klasycznych filtrów.'
}

export default function AskPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const message = searchParams.get('q')?.trim() ?? ''
  const listingId = searchParams.get('listing')
  const page = Number.parseInt(searchParams.get('page') ?? '1', 10) || 1
  const [draft, setDraft] = useState<string>()
  const [exclusion, setExclusion] = useState({ message: '', indexes: [] as number[] })
  const excludeNeeds = exclusion.message === message ? exclusion.indexes : []
  const maxLength = useQuery(metaQuery).data?.chatMaxLength

  const queryClient = useQueryClient()
  const askKey = ['ask', message, excludeNeeds]

  const ask = useQuery({
    queryKey: askKey,
    queryFn: ({ signal }) => postAsk(message, excludeNeeds, false, signal),
    enabled: message !== '',
    retry: false,
    staleTime: Infinity,
    placeholderData: (previous, previousQuery) => (previousQuery?.queryKey[1] === message ? previous : undefined),
  })

  const more = useMutation({
    mutationFn: () => postAsk(message, excludeNeeds, true),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({ queryKey: ['ask', message], refetchType: 'none' })
      queryClient.setQueryData(askKey, data)
    },
  })

  const without = (...keys: string[]) => {
    const next = new URLSearchParams(searchParams)
    keys.forEach((key) => next.delete(key))
    return next
  }

  const clearQuestion = () => {
    setDraft(undefined)
    setSearchParams(without('q', 'page'))
  }

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const text = (draft ?? message).trim()
    if (!text) {
      clearQuestion()
      return
    }
    setDraft(undefined)
    if (text === message && excludeNeeds.length === 0) {
      void ask.refetch()
      return
    }
    setExclusion({ message: text, indexes: [] })
    const next = without('listing', 'page')
    next.set('q', text)
    setSearchParams(next)
  }

  const excludeNeed = (index: number) => setExclusion({ message, indexes: [...excludeNeeds, index] })
  const restoreNeeds = () => setExclusion({ message, indexes: [] })

  useEffect(() => {
    window.scrollTo(0, 0)
  }, [page, listingId])

  if (listingId !== null) {
    return (
      <section className="panel" aria-label="Oferty">
        <ListingDetailPage id={listingId} onClose={() => setSearchParams(without('listing'))} />
      </section>
    )
  }

  return (
    <div className="container ask">
      <div className="ask__head">
        <h1 id="ask-heading" className={message === '' ? 'ask__heading' : 'sr-only'}>
          Opisz, czego szukasz. Resztę przeczytamy za Ciebie.
        </h1>
        <form className="composer__form" role="search" onSubmit={submit}>
          <input
            className="composer__input"
            type="text"
            aria-labelledby="ask-heading"
            placeholder="np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku do 700 tys."
            maxLength={maxLength}
            value={draft ?? message}
            onChange={(event) => setDraft(event.target.value)}
          />
          {message !== '' && (
            <button className="composer__clear" type="button" aria-label="Wyczyść pytanie" onClick={clearQuestion}>
              ×
            </button>
          )}
          <button className="composer__send" type="submit">
            Szukaj
          </button>
        </form>
      </div>
      {message !== '' && (
        <section className="results ask__results" aria-busy={ask.isFetching} aria-label="Oferty">
          {ask.isPending && <AskProgress key={`${message}:${excludeNeeds.join(',')}`} />}
          {ask.isError && (
            <div className="notice notice--error" role="alert">
              {askErrorMessage(ask.error, maxLength)}
            </div>
          )}
          {ask.data && (
            <div className={ask.isPlaceholderData ? 'ask__answer cards--stale' : 'ask__answer'}>
              <AskReply
                response={ask.data}
                excludeNeeds={excludeNeeds}
                onExcludeNeed={excludeNeed}
                onRestoreNeeds={restoreNeeds}
              />
              <AskResults
                response={ask.data}
                excludeNeeds={excludeNeeds}
                page={page}
                checkingMore={more.isPending}
                onCheckMore={() => more.mutate()}
              />
              {more.isError && (
                <div className="notice notice--error" role="alert">
                  {askErrorMessage(more.error, maxLength)}
                </div>
              )}
            </div>
          )}
          {ask.isPending && (
            <ul className="ask-list" aria-busy="true">
              {[1, 2, 3, 4].map((placeholder) => (
                <li key={placeholder}>
                  <div className="tile tile--skeleton" />
                </li>
              ))}
            </ul>
          )}
        </section>
      )}
    </div>
  )
}
