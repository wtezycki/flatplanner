import { useSearchParams } from 'react-router'
import type { ListingItem } from '../api.ts'
import { listingSearch } from '../intent.ts'
import type { CardLabels } from '../useCardLabels.ts'
import ListingCardBody from './ListingCardBody.tsx'
import PhotoCarousel from './PhotoCarousel.tsx'

interface Props {
  item: ListingItem
  labels: CardLabels
}

export default function ListingCard({ item, labels }: Props) {
  const [searchParams] = useSearchParams()
  const href = { search: listingSearch(searchParams, item.id) }

  return (
    <article className="card">
      <PhotoCarousel urls={item.imageUrls} href={href} />
      <ListingCardBody item={item} labels={labels} href={href} />
    </article>
  )
}
