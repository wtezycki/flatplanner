import { useState } from 'react'
import type { FormEvent } from 'react'
import { useSearchParams } from 'react-router'

const TEXT_MAX_LENGTH = 100

export default function SearchBar() {
  const [searchParams, setSearchParams] = useSearchParams()
  const [draft, setDraft] = useState<string>()
  const value = draft ?? searchParams.get('text') ?? ''

  const submit = (event: FormEvent) => {
    event.preventDefault()
    const text = value.trim()
    setDraft(undefined)
    const next = new URLSearchParams(searchParams)
    if (text) {
      next.set('text', text)
    } else {
      next.delete('text')
    }
    next.delete('page')
    setSearchParams(next)
  }

  return (
    <form className="search" role="search" onSubmit={submit}>
      <input
        className="field search__input"
        type="search"
        aria-label="Szukaj w ofertach"
        placeholder="np. Morena, Wrzeszcz, balkon"
        maxLength={TEXT_MAX_LENGTH}
        value={value}
        onChange={(event) => setDraft(event.target.value)}
      />
      <button className="btn btn--primary" type="submit">
        Szukaj
      </button>
    </form>
  )
}
