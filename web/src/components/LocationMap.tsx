import L from 'leaflet'
import 'leaflet/dist/leaflet.css'
import { useEffect, useRef } from 'react'

const APPROXIMATE_RADIUS_M = 500
const APPROXIMATE_ZOOM = 14
const EXACT_ZOOM = 16
const MARKER_COLOR = '#007c68'
const TILE_URL = 'https://tile.openstreetmap.org/{z}/{x}/{y}.png'
const ATTRIBUTION = '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
const PREVIEW_OPTIONS: L.MapOptions = {
  zoomControl: false,
  dragging: false,
  scrollWheelZoom: false,
  doubleClickZoom: false,
  boxZoom: false,
  keyboard: false,
  touchZoom: false,
}

interface Props {
  lat: number
  lng: number
  approximate: boolean
  interactive: boolean
}

export default function LocationMap({ lat, lng, approximate, interactive }: Props) {
  const container = useRef<HTMLDivElement>(null)

  useEffect(() => {
    if (!container.current) {
      return
    }
    const map = L.map(container.current, interactive ? {} : PREVIEW_OPTIONS).setView(
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
    const frame = requestAnimationFrame(() => map.invalidateSize())
    return () => {
      cancelAnimationFrame(frame)
      map.remove()
    }
  }, [lat, lng, approximate, interactive])

  return (
    <div
      ref={container}
      className={interactive ? 'map map--full' : 'map'}
      role="region"
      aria-label="Mapa lokalizacji"
    />
  )
}
