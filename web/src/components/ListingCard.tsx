import { useRef, useState } from 'react'
import { Link, useNavigate } from 'react-router'
import type { ListingItem } from '../api.ts'
import { formatArea, formatFloor, formatPrice, formatPricePerM2, formatRooms } from '../format.ts'

interface CarouselProps {
  urls: string[]
  onOpen: () => void
}

function PhotoCarousel({ urls, onOpen }: CarouselProps) {
  const track = useRef<HTMLDivElement>(null)
  const [index, setIndex] = useState(0)
  const [failed, setFailed] = useState<string[]>([])
  const photos = urls.filter((url) => !failed.includes(url))
  const current = Math.min(index, photos.length - 1)

  if (photos.length === 0) {
    return <div className="gallery gallery--missing">Brak zdjęcia</div>
  }

  const show = (target: number) => {
    setIndex(target)
    track.current?.scrollTo({ left: target * track.current.clientWidth })
  }

  return (
    <div className="gallery" role="group" aria-label="Zdjęcia oferty">
      <div
        ref={track}
        className="gallery__track"
        tabIndex={0}
        onClick={onOpen}
        onScroll={(event) => setIndex(Math.round(event.currentTarget.scrollLeft / event.currentTarget.clientWidth))}
      >
        {photos.map((url) => (
          <img
            key={url}
            className="gallery__image"
            src={url}
            alt=""
            loading="lazy"
            referrerPolicy="no-referrer"
            onError={() => setFailed((current) => [...current, url])}
          />
        ))}
      </div>
      {photos.length > 1 && (
        <>
          <button
            className="gallery__nav gallery__nav--prev"
            type="button"
            aria-label="Poprzednie zdjęcie"
            disabled={current === 0}
            onClick={() => show(current - 1)}
          >
            ‹
          </button>
          <button
            className="gallery__nav gallery__nav--next"
            type="button"
            aria-label="Następne zdjęcie"
            disabled={current === photos.length - 1}
            onClick={() => show(current + 1)}
          >
            ›
          </button>
          <span className="gallery__counter num">
            {current + 1} / {photos.length}
          </span>
        </>
      )}
    </div>
  )
}

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
