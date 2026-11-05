import { keepPreviousData, useQuery } from '@tanstack/react-query'
import { Link, useSearchParams } from 'react-router'
import { ApiError, fetchListings, metaQuery } from '../api.ts'
import Filters from '../components/Filters.tsx'
import ListingCard from '../components/ListingCard.tsx'
import Pagination from '../components/Pagination.tsx'
import SearchBar from '../components/SearchBar.tsx'
import { formatListingsCount } from '../format.ts'
import { intentFromSearchParams } from '../intent.ts'

const DEFAULT_RETRY_AFTER_SECONDS = 10
const SKELETON_CARDS = 3

function ListError({ error, onRetry }: { error: Error; onRetry: () => void }) {
  if (error instanceof ApiError && error.status === 400) {
    return (
      <div className="notice notice--error">
        <p>Nieprawidłowe parametry wyszukiwania.</p>
        <Link to="/">Wyczyść wyszukiwanie</Link>
      </div>
    )
  }
  if (error instanceof ApiError && error.status === 429) {
    return (
      <div className="notice notice--warning">
        <p>Za dużo zapytań. Spróbuj ponownie za {error.retryAfter ?? DEFAULT_RETRY_AFTER_SECONDS} s.</p>
      </div>
    )
  }
  return (
    <div className="notice notice--error">
      <p>Nie udało się pobrać ofert.</p>
      <button className="btn btn--secondary" type="button" onClick={onRetry}>
        Spróbuj ponownie
      </button>
    </div>
  )
}

function Skeleton() {
  return (
    <ul className="cards">
      {Array.from({ length: SKELETON_CARDS }, (_, index) => (
        <li key={index} className="card card--skeleton" />
      ))}
    </ul>
  )
}

export default function ListingsPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const intent = intentFromSearchParams(searchParams)
  const meta = useQuery(metaQuery)
  const listings = useQuery({
    queryKey: ['listings', searchParams.toString()],
    queryFn: ({ signal }) => fetchListings(searchParams, signal),
    placeholderData: keepPreviousData,
  })
  const { data } = listings

  const sortOptions = (meta.data?.enums.sort ?? []).filter(
    (option) => option.value !== 'relevance' || intent.preferences.length > 0,
  )
  const marketLabel = (value: string) => meta.data?.enums.market.find((option) => option.value === value)?.label

  const changeSort = (sort: string) => {
    const next = new URLSearchParams(searchParams)
    next.set('sort', sort)
    next.delete('page')
    setSearchParams(next)
  }

  let countLabel = ''
  if (data) {
    countLabel = formatListingsCount(data.total)
  } else if (listings.isPending) {
    countLabel = 'Ładowanie…'
  }

  let body
  if (listings.isError) {
    body = <ListError error={listings.error} onRetry={() => void listings.refetch()} />
  } else if (!data) {
    body = <Skeleton />
  } else if (data.total === 0) {
    body = (
      <div className="empty">
        <p className="empty__title">Brak ofert dla tych kryteriów.</p>
      </div>
    )
  } else {
    body = (
      <>
        <ul className={listings.isPlaceholderData ? 'cards cards--stale' : 'cards'}>
          {data.items.map((item) => (
            <li key={item.id}>
              <ListingCard item={item} marketLabel={marketLabel(item.market)} />
            </li>
          ))}
        </ul>
        <Pagination page={data.page} totalPages={data.totalPages} />
      </>
    )
  }

  return (
    <>
      <SearchBar />
      {data?.interpretation.warnings.map((warning) => (
        <div key={`${warning.field}:${warning.message}`} className="notice notice--warning">
          <span className="sr-only">Uwaga: </span>
          {warning.message}
        </div>
      ))}
      <div className="layout">
        <Filters />
        <section className="results" aria-busy={listings.isPending}>
          <div className="results__bar">
            <p className="results__count" aria-live="polite">
              {countLabel}
            </p>
            {sortOptions.length > 0 && (
              <label className="results__sort">
                Sortuj
                <select
                  className="field"
                  value={data?.sort ?? searchParams.get('sort') ?? 'newest'}
                  onChange={(event) => changeSort(event.target.value)}
                >
                  {sortOptions.map((option) => (
                    <option key={option.value} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
            )}
          </div>
          {body}
        </section>
      </div>
    </>
  )
}
