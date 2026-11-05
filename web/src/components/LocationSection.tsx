import { lazy, Suspense, useState } from 'react'
import FullscreenDialog from './FullscreenDialog.tsx'

const LocationMap = lazy(() => import('./LocationMap.tsx'))

interface Props {
  lat: number
  lng: number
  approximate: boolean
}

export default function LocationSection({ lat, lng, approximate }: Props) {
  const [expanded, setExpanded] = useState(false)

  return (
    <section>
      <h2 className="detail__heading">Lokalizacja</h2>
      <div className="location__preview">
        <Suspense fallback={<div className="map" />}>
          <LocationMap lat={lat} lng={lng} approximate={approximate} interactive={false} />
        </Suspense>
        <button className="location__open" type="button" aria-label="Powiększ mapę" onClick={() => setExpanded(true)}>
          <span className="location__hint">Powiększ mapę</span>
        </button>
      </div>
      {approximate && <p className="detail__map-note">Lokalizacja przybliżona</p>}
      {expanded && (
        <FullscreenDialog label="Mapa lokalizacji" onClose={() => setExpanded(false)}>
          <Suspense>
            <LocationMap lat={lat} lng={lng} approximate={approximate} interactive />
          </Suspense>
        </FullscreenDialog>
      )}
    </section>
  )
}
