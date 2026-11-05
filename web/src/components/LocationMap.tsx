import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useRef } from 'react'

const APPROXIMATE_RADIUS_M = 500
const APPROXIMATE_ZOOM = 14
const EXACT_ZOOM = 16
const MARKER_COLOR = '#007c68'
const TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'

interface Props {
  lat: number
  lng: number
  approximate: boolean
}

export default function LocationMap({ lat, lng, approximate }: Props) {
  const container = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!container.current) {
      return
    }
    const map = L.map(container.current, { scrollWheelZoom: false }).setView(
      [lat, lng],
      approximate ? APPROXIMATE_ZOOM : EXACT_ZOOM,
    )
    L.tileLayer(TILE_URL, { maxZoom: 19, attribution: ATTRIBUTION }).addTo(map)
    const style = { color: MARKER_COLOR, fillColor: MARKER_COLOR, fillOpacity: 0.2 }
    if (approximate) {
      L.circle([lat, lng], { ...style, radius: APPROXIMATE_RADIUS_M }).addTo(map)
    } else {
      L.circleMarker([lat, lng], { ...style, fillOpacity: 0.9, radius: 9 }).addTo(map)
    }
    return () => {
      map.remove()
    }
  }, [lat, lng, approximate])

  return <div ref={container} className="map" role="region" aria-label="Mapa lokalizacji" />
}
