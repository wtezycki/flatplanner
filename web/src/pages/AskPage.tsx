import { useQuery } from '@tanstack/react-query'
import { useEffect, useRef, useState } from 'react'
import type { ReactNode } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import logo from '../assets/flatplanner.svg'
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
  const panelRef = useRef<HTMLElement>(null)
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
    const text = (draft ?? '').trim()
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
    panelRef.current?.scrollTo(0, 0)
  }, [page, listingId, classic])

  let panel: ReactNode = null
  if (listingId !== null) {
    panel = <ListingDetailPage id={listingId} onClose={() => setSearchParams(without('listing'))} />
  } else if (classic) {
    panel = <ListingsPage onBack={message === '' ? undefined : () => setSearchParams(without('view', 'page'))} />
  } else if (ask.data) {
    panel = <AskResults response={ask.data} page={page} />
  } else if (message !== '') {
    panel = (
      <ul className="ask-list" aria-busy="true">
        {[1, 2, 3, 4].map((placeholder) => (
          <li key={placeholder}>
            <div className="tile tile--skeleton" />
          </li>
        ))}
      </ul>
    )
  }

  return (
    <div className={panel === null ? 'workspace workspace--empty' : 'workspace'}>
      <section className="chat" aria-label="Rozmowa">
        <h1 id="ask-heading" className={message === '' ? 'chat__heading' : 'sr-only'}>
          Opisz, czego szukasz. Resztę przeczytamy za Ciebie.
        </h1>
        {message !== '' && (
          <div className="chat__thread">
            <p className="bubble">{message}</p>
            <div className="reply">
              <img className="reply__avatar" src={logo} alt="" />
              <div className="reply__body">
                {ask.isPending && <AskProgress key={`${message}:${excludeNeeds.join(',')}`} />}
                {ask.isError && (
                  <div className="notice notice--error" role="alert">
                    {askErrorMessage(ask.error, maxLength)}
                  </div>
                )}
                {ask.data && <AskReply response={ask.data} onExcludeNeed={excludeNeed} />}
              </div>
            </div>
          </div>
        )}
        <div className="composer">
          <form className="composer__form" role="search" onSubmit={submit}>
            <input
              className="composer__input"
              type="text"
              aria-labelledby="ask-heading"
              placeholder="np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku do 700 tys."
              maxLength={maxLength}
              value={draft ?? ''}
              onChange={(event) => setDraft(event.target.value)}
            />
            <button className="composer__send" type="submit">
              Szukaj
            </button>
          </form>
          {!classic && (
            <p className="composer__classic">
              <Link to={{ search: classicSearch }}>Wolisz klasyczne filtry?</Link>
            </p>
          )}
        </div>
      </section>
      {panel !== null && (
        <section className="panel" ref={panelRef} aria-label="Oferty">
          {panel}
        </section>
      )}
    </div>
  )
}
