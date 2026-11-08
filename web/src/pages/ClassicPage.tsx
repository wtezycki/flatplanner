import { useEffect } from 'react'
import { useSearchParams } from 'react-router'
import Filters from '../components/Filters.tsx'
import SearchBar from '../components/SearchBar.tsx'
import ListingDetailPage from './ListingDetailPage.tsx'
import ListingsPage from './ListingsPage.tsx'

export default function ClassicPage() {
  const [searchParams, setSearchParams] = useSearchParams()
  const listingId = searchParams.get('listing')
  const page = searchParams.get('page')

  useEffect(() => {
    window.scrollTo(0, 0)
  }, [page, listingId])

  if (listingId !== null) {
    const closeListing = () => {
      const next = new URLSearchParams(searchParams)
      next.delete('listing')
      setSearchParams(next)
    }
    return (
      <section className="panel" aria-label="Oferty">
        <ListingDetailPage id={listingId} onClose={closeListing} />
      </section>
    )
  }

  return (
    <div className="container">
      <div className="layout">
        <div className="layout__side">
          <SearchBar />
          <Filters />
        </div>
        <ListingsPage />
      </div>
    </div>
  )
}
