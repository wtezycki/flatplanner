import { useQuery } from '@tanstack/react-query'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { ApiError, DEFAULT_RETRY_AFTER_SECONDS, metaQuery, postAsk } from '../api.ts'
import AskProgress from '../components/AskProgress.tsx'
import AskReply from '../components/AskReply.tsx'
import AskResults from '../components/AskResults.tsx'
import ListingDetailPage from './ListingDetailPage.tsx'
import ListingsPage from './ListingsPage.tsx'

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
  const classic = searchParams.get('view') === 'classic'
  const page = Number.parseInt(searchParams.get('page') ?? '1', 10) || 1
  const [draft, setDraft] = useState<string>()
  const [exclusion, setExclusion] = useState({ message: '', indexes: [] as number[] })
  const excludeNeeds = exclusion.message === message ? exclusion.indexes : []
  const maxLength = useQuery(metaQuery).data?.chatMaxLength

  const ask = useQuery({
    queryKey: ['ask', message, excludeNeeds],
    queryFn: ({ signal }) => postAsk(message, excludeNeeds, signal),
    enabled: message !== '',
    retry: false,
    staleTime: Infinity,
  })

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const text = (draft ?? message).trim()
    if (!text) {
      return
    }
    setDraft(undefined)
    if (text === message && excludeNeeds.length === 0) {
      void ask.refetch()
      return
    }
    setExclusion({ message: text, indexes: [] })
    setSearchParams({ q: text })
  }

  const excludeNeed = (index: number) => setExclusion({ message, indexes: [...excludeNeeds, index] })
  const restoreNeeds = () => setExclusion({ message, indexes: [] })

  const without = (...keys: string[]) => {
    const next = new URLSearchParams(searchParams)
    keys.forEach((key) => next.delete(key))
    return next
  }
  const classicSearch = (() => {
    const next = without('listing', 'page')
    next.set('view', 'classic')
    return next.toString()
  })()

  useEffect(() => {
    window.scrollTo(0, 0)
  }, [page, listingId, classic])

  if (listingId !== null) {
    return (
      <section className="panel" aria-label="Oferty">
        <ListingDetailPage id={listingId} onClose={() => setSearchParams(without('listing'))} />
      </section>
    )
  }
  if (classic) {
    return (
      <section className="panel" aria-label="Oferty">
        <ListingsPage onBack={message === '' ? undefined : () => setSearchParams(without('view', 'page'))} />
      </section>
    )
  }

  const empty = message === ''

  return (
    <div className={empty ? 'ask ask--empty' : 'ask'}>
      <div className="container ask__head">
        <h1 id="ask-heading" className={empty ? 'ask__heading' : 'sr-only'}>
          Opisz, czego szukasz. Resztę przeczytamy za Ciebie.
        </h1>
        <div className="composer">
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
            <button className="composer__send" type="submit">
              Szukaj
            </button>
          </form>
          <p className="composer__classic">
            <Link to={{ search: classicSearch }}>Wolisz klasyczne filtry?</Link>
          </p>
        </div>
        {!empty && (
          <div className="ask__reply">
            {ask.isPending && <AskProgress key={`${message}:${excludeNeeds.join(',')}`} />}
            {ask.isError && (
              <div className="notice notice--error" role="alert">
                {askErrorMessage(ask.error, maxLength)}
              </div>
            )}
            {ask.data && (
              <AskReply
                response={ask.data}
                excludeNeeds={excludeNeeds}
                onExcludeNeed={excludeNeed}
                onRestoreNeeds={restoreNeeds}
              />
            )}
          </div>
        )}
      </div>
      {!empty && (
        <section className="container panel" aria-label="Oferty">
          {ask.data && <AskResults response={ask.data} excludeNeeds={excludeNeeds} page={page} />}
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
