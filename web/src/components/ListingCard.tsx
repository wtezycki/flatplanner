import { useState } from 'react'
import { Link } from 'react-router'
import type { ListingItem } from '../api.ts'
import { formatArea, formatFloor, formatPrice, formatPricePerM2, formatRooms } from '../format.ts'

interface Props {
  item: ListingItem
  marketLabel?: string
}

export default function ListingCard({ item, marketLabel }: Props) {
  const [imageFailed, setImageFailed] = useState(false)
  const parameters = [
    formatArea(item.areaM2),
    formatRooms(item.rooms),
    item.floor === undefined ? undefined : formatFloor(item.floor, item.buildingFloors, item.floorHigherThan10),
  ].filter(Boolean)
  const location = item.districtName ? `${item.districtName}, ${item.cityName}` : item.cityName

  return (
    <Link to={`/listings/${item.id}`} className="card">
      {item.imageUrl && !imageFailed ? (
        <img
          className="card__image"
          src={item.imageUrl}
          alt=""
          loading="lazy"
          referrerPolicy="no-referrer"
          onError={() => setImageFailed(true)}
        />
      ) : (
        <div className="card__image card__image--missing">Brak zdjęcia</div>
      )}
      <div className="card__body">
        <p className="card__price-line">
          <span className="card__price num">{formatPrice(item.pricePln)}</span>
          <span className="card__price-per-m2 num">{formatPricePerM2(item.pricePerM2)}</span>
        </p>
        <p className="card__parameters">{parameters.join(' · ')}</p>
        <p className="card__location">{location}</p>
        <p className="card__title">{item.title}</p>
        <p className="card__badges">
          {marketLabel && <span className="badge">{marketLabel}</span>}
          {item.locationApproximate && <span className="badge">lokalizacja przybliżona</span>}
          {item.priceOutlier && <span className="badge badge--warning">nietypowa cena, sprawdź opis</span>}
        </p>
      </div>
    </Link>
  )
}
