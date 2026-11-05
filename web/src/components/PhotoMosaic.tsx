import { useState } from 'react'
import { formatPhotosCount } from '../format.ts'

const MAX_TILES = 3

interface Props {
  urls: string[]
  onOpen: (url: string) => void
}

export default function PhotoMosaic({ urls, onOpen }: Props) {
  const [failed, setFailed] = useState<string[]>([])
  const photos = urls.filter((url) => !failed.includes(url))
  const tiles = photos.slice(0, MAX_TILES)
  const hidden = photos.length - tiles.length

  if (tiles.length === 0) {
    return <div className="mosaic mosaic--missing">Brak zdjęcia</div>
  }

  return (
    <div className={`mosaic mosaic--${tiles.length}`} role="group" aria-label="Zdjęcia oferty">
      {tiles.map((url, index) => (
        <button
          key={url}
          className="mosaic__tile"
          type="button"
          aria-label={`Powiększ zdjęcie ${index + 1} z ${photos.length}`}
          onClick={() => onOpen(url)}
        >
          <img
            src={url}
            alt=""
            referrerPolicy="no-referrer"
            onError={() => setFailed((current) => [...current, url])}
          />
          {index === tiles.length - 1 && hidden > 0 && (
            <span className="mosaic__more">+{formatPhotosCount(hidden)}</span>
          )}
        </button>
      ))}
    </div>
  )
}
