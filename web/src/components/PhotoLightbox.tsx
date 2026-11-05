import { useState, type KeyboardEvent } from 'react'
import FullscreenDialog from './FullscreenDialog.tsx'

interface Props {
  urls: string[]
  startUrl: string
  title: string
  onClose: () => void
}

export default function PhotoLightbox({ urls, startUrl, title, onClose }: Props) {
  const [index, setIndex] = useState(Math.max(urls.indexOf(startUrl), 0))

  const step = (delta: number) => setIndex((current) => (current + delta + urls.length) % urls.length)

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === 'ArrowLeft') {
      step(-1)
    } else if (event.key === 'ArrowRight') {
      step(1)
    }
  }

  return (
    <FullscreenDialog label="Zdjęcia oferty" onClose={onClose} onKeyDown={onKeyDown}>
      <img
        className="lightbox__image"
        src={urls[index]}
        alt={`${title}, zdjęcie ${index + 1} z ${urls.length}`}
        referrerPolicy="no-referrer"
      />
      {urls.length > 1 && (
        <>
          <button
            className="lightbox__button lightbox__nav lightbox__nav--prev"
            type="button"
            aria-label="Poprzednie zdjęcie"
            onClick={() => step(-1)}
          >
            ‹
          </button>
          <button
            className="lightbox__button lightbox__nav lightbox__nav--next"
            type="button"
            aria-label="Następne zdjęcie"
            onClick={() => step(1)}
          >
            ›
          </button>
          <span className="lightbox__counter num">
            {index + 1} / {urls.length}
          </span>
        </>
      )}
    </FullscreenDialog>
  )
}
