import { useRef, useState } from 'react'
import type { MouseEvent, PointerEvent } from 'react'
import { Link } from 'react-router'
import type { To } from 'react-router'

const DRAG_THRESHOLD_PX = 5

interface Props {
  urls: string[]
  onOpen?: (url: string) => void
  href?: To
}

export default function PhotoCarousel({ urls, onOpen, href }: Props) {
  const track = useRef<HTMLDivElement>(null)
  const drag = useRef<{ x: number; left: number; moved: boolean }>(undefined)
  const dragged = useRef(false)
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

  const startDrag = (event: PointerEvent<HTMLDivElement>) => {
    if (event.pointerType === 'mouse' && event.button === 0 && track.current) {
      drag.current = { x: event.clientX, left: track.current.scrollLeft, moved: false }
    }
  }

  const moveDrag = (event: PointerEvent<HTMLDivElement>) => {
    const element = track.current
    if (!drag.current || !element) {
      return
    }
    const distance = event.clientX - drag.current.x
    if (!drag.current.moved && Math.abs(distance) > DRAG_THRESHOLD_PX) {
      drag.current.moved = true
      element.setPointerCapture(event.pointerId)
      element.classList.add('gallery__track--dragging')
    }
    if (drag.current.moved) {
      element.scrollLeft = drag.current.left - distance
    }
  }

  const endDrag = () => {
    const element = track.current
    if (drag.current?.moved && element) {
      element.classList.remove('gallery__track--dragging')
      dragged.current = true
      show(Math.min(photos.length - 1, Math.max(0, Math.round(element.scrollLeft / element.clientWidth))))
    }
    drag.current = undefined
  }

  const swallowClickAfterDrag = (event: MouseEvent) => {
    if (dragged.current) {
      event.preventDefault()
      event.stopPropagation()
      dragged.current = false
      return true
    }
    return false
  }

  const trackElement = (
    <div
      ref={track}
      className="gallery__track"
      tabIndex={href ? undefined : 0}
      onClick={(event) => {
        if (!swallowClickAfterDrag(event)) {
          onOpen?.(photos[current])
        }
      }}
      onPointerDown={startDrag}
      onPointerMove={moveDrag}
      onPointerUp={endDrag}
      onPointerCancel={endDrag}
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
        <Link className="gallery__link" to={href} draggable={false} onClick={(event) => void swallowClickAfterDrag(event)}>
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
