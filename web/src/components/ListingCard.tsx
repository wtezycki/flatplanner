import { Link, useNavigate } from 'react-router'
import type { ListingItem } from '../api.ts'
import { formatArea, formatFloor, formatPrice, formatPricePerM2, formatRooms } from '../format.ts'
import PhotoCarousel from './PhotoCarousel.tsx'

interface Props {
  item: ListingItem
  marketLabel?: string
}

export default function ListingCard({ item, marketLabel }: Props) {
  const navigate = useNavigate()
  const href = `/listings/${item.id}`
  const parameters = [
    formatArea(item.areaM2),
    formatRooms(item.rooms),
    item.floor === undefined ? undefined : formatFloor(item.floor, item.buildingFloors, item.floorHigherThan10),
  ].filter(Boolean)
  const location = item.districtName ? `${item.districtName}, ${item.cityName}` : item.cityName

  return (
    <article className="card">
      <PhotoCarousel urls={item.imageUrls} onOpen={() => void navigate(href)} />
      <div className="card__body">
        <p className="card__price-line">
          <span className="card__price num">{formatPrice(item.pricePln)}</span>
          <span className="card__price-per-m2 num">{formatPricePerM2(item.pricePerM2)}</span>
        </p>
        <p className="card__parameters">{parameters.join(' · ')}</p>
        <p className="card__location">{location}</p>
        <p className="card__title">
          <Link className="card__link" to={href}>
            {item.title}
          </Link>
        </p>
        <p className="card__badges">
          {marketLabel && <span className="badge">{marketLabel}</span>}
          {item.locationApproximate && <span className="badge">lokalizacja przybliżona</span>}
          {item.priceOutlier && <span className="badge badge--warning">nietypowa cena, sprawdź opis</span>}
        </p>
      </div>
    </article>
  )
}
