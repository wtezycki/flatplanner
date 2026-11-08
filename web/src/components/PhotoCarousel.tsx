import { useRef, useState } from 'react'
import { Link } from 'react-router'
import type { To } from 'react-router'

interface Props {
  urls: string[]
  onOpen?: (url: string) => void
  href?: To
}

export default function PhotoCarousel({ urls, onOpen, href }: Props) {
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

  const trackElement = (
    <div
      ref={track}
      className="gallery__track"
      tabIndex={href ? undefined : 0}
      onClick={() => onOpen?.(photos[current])}
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
          draggable={false}
          onError={() => setFailed((current) => [...current, url])}
        />
      ))}
    </div>
  )

  return (
    <div className="gallery" role="group" aria-label="Zdjęcia oferty">
      {href ? (
        <Link className="gallery__link" to={href}>
          {trackElement}
        </Link>
      ) : (
        trackElement
      )}
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
