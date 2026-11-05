import { useRef, useState } from 'react'

interface Props {
  urls: string[]
  onOpen?: (url: string) => void
}

export default function PhotoCarousel({ urls, onOpen }: Props) {
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
