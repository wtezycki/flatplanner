import { useQuery, useQueryClient } from '@tanstack/react-query'
import { useEffect, useRef } from 'react'
import { useSearchParams } from 'react-router'
import { metaQuery, threadQuery } from '../api.ts'
import ChatTurn from '../components/ChatTurn.tsx'
import Composer from '../components/Composer.tsx'
import Icon from '../components/Icon.tsx'
import ListingDetailPage from './ListingDetailPage.tsx'

export default function AskPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const queryClient = useQueryClient()
  const meta = useQuery(metaQuery).data
  const turns = useQuery(threadQuery).data
  const maxTurns = meta?.chatMaxTurns
  const listingId = searchParams.get('listing')
  const page = Number.parseInt(searchParams.get('page') ?? '1', 10) || 1
  const maxLength = meta?.chatMaxLength
  const thread = useRef<HTMLDivElement>(null)
  const previousTurns = useRef(turns.length)
  const previousPage = useRef(page)
  const limitReached = maxTurns !== undefined && turns.length >= maxTurns

  const setTurns = (next: string[]) => {
    queryClient.setQueryData(threadQuery.queryKey, next)
    if (searchParams.has('page')) {
      setSearchParams(without('page'))
    }
  }

  const ask = (text: string) => setTurns([...turns, text])

  const without = (...keys: string[]) => {
    const next = new URLSearchParams(searchParams)
    keys.forEach((key) => next.delete(key))
    return next
  }

  useEffect(() => {
    window.scrollTo(0, 0)
  }, [listingId])

  useEffect(() => {
    if (page !== previousPage.current) {
      thread.current?.lastElementChild?.scrollIntoView({ block: 'start' })
    }
    previousPage.current = page
  }, [page])

  useEffect(() => {
    if (turns.length > previousTurns.current) {
      thread.current?.lastElementChild?.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
    previousTurns.current = turns.length
  }, [turns.length])

  if (listingId !== null) {
    return (
      <section className="panel" aria-label="Oferty">
        <ListingDetailPage id={listingId} onClose={() => setSearchParams(without('listing'))} />
      </section>
    )
  }

  if (turns.length === 0) {
    return (
      <div className="container chat chat--landing">
        <div className="hero">
          <span className="hero__badge" aria-hidden="true">
            <Icon name="sparkles" />
          </span>
          <h1 className="hero__title">
            Opisz, czego szukasz.
            <span className="hero__accent"> Resztę przeczytamy za Ciebie.</span>
          </h1>
          <Composer
            label="Opisz, czego szukasz"
            placeholder="np. Pracuję na nocki i mam psa, szukam czegoś w Gdańsku do 700 tys."
            maxLength={maxLength}
            autoFocus
            onSubmit={ask}
          />
        </div>
      </div>
    )
  }

  return (
    <div className="chat">
      <div className="container chat__container" ref={thread}>
        {turns.map((message, index) => (
          <ChatTurn
            key={`${index}:${message}`}
            history={turns.slice(0, index)}
            message={message}
            isLast={index === turns.length - 1}
            page={page}
          />
        ))}
      </div>
      <div className="dock">
        <div className="container dock__inner">
          <button className="dock__new" type="button" aria-label="Nowa rozmowa" title="Nowa rozmowa" onClick={() => setTurns([])}>
            <Icon name="newChat" />
          </button>
          {limitReached ? (
            <p className="dock__limit">Rozmowa może mieć najwyżej {maxTurns} pytań. Zacznij nową.</p>
          ) : (
            <Composer label="Dopytaj albo zmień kryteria" placeholder="Dopytaj albo zmień kryteria" maxLength={maxLength} autoFocus onSubmit={ask} />
          )}
        </div>
      </div>
    </div>
  )
}
