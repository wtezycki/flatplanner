import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import type { FormEvent } from 'react'
import { Link, useSearchParams } from 'react-router'
import { ApiError, DEFAULT_RETRY_AFTER_SECONDS, metaQuery, postAsk } from '../api.ts'
import AskProgress from '../components/AskProgress.tsx'
import AskResults from '../components/AskResults.tsx'

const EXAMPLES = ['Ukrainiec, szkoła na Morenie', 'Muzyk, ćwiczę codziennie', 'Pies i park', 'Rodzice na wózku']

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

  return (
    <>
      <section className="hero">
        <div className="container hero__inner">
          <h1 id="ask-heading" className="hero__heading">Opisz, czego szukasz. Resztę przeczytamy za Ciebie.</h1>
          <form className="hero__form" role="search" onSubmit={submit}>
            <input
              className="field hero__input"
              type="search"
              aria-labelledby="ask-heading"
              placeholder="np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku do 700 tys."
              maxLength={maxLength}
              value={draft ?? message}
              onChange={(event) => setDraft(event.target.value)}
            />
            <button className="btn btn--primary hero__button" type="submit">
              Szukaj
            </button>
          </form>
          <ul className="hero__examples">
            {EXAMPLES.map((example) => (
              <li key={example}>
                <button className="hero__example" type="button" onClick={() => setDraft(example)}>
                  {example}
                </button>
              </li>
            ))}
          </ul>
          {ask.isError && (
            <div className="notice notice--error" role="alert">
              {askErrorMessage(ask.error, maxLength)}
            </div>
          )}
        </div>
      </section>
      <div className="container ask">
        {ask.isPending && message !== '' && <AskProgress key={`${message}:${excludeNeeds.join(',')}`} />}
        {ask.data && <AskResults response={ask.data} onExcludeNeed={excludeNeed} />}
        <p className="ask__classic">
          <Link to="/listings">Wolisz klasyczne filtry?</Link>
        </p>
      </div>
    </>
  )
}
