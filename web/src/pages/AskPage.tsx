import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import logo from '../assets/flatplanner.svg'
import { ApiError, DEFAULT_RETRY_AFTER_SECONDS, metaQuery, postAsk } from '../api.ts'
import AskProgress from '../components/AskProgress.tsx'
import AskResults from '../components/AskResults.tsx'

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

  return (
    <div className={message === '' ? 'container chat chat--empty' : 'container chat'}>
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
              {ask.data && <AskResults response={ask.data} onExcludeNeed={excludeNeed} />}
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
        <p className="composer__classic">
          <Link to="/listings">Wolisz klasyczne filtry?</Link>
        </p>
      </div>
    </div>
  )
}
