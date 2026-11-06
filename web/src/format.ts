const integerFormat = new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 0 })
const decimalFormat = new Intl.NumberFormat('pl-PL', { maximumFractionDigits: 2 })

export function pluralPl(count: number, one: string, few: string, many: string): string {
  if (count === 1) {
    return one
  }
  const lastDigit = count % 10
  const lastTwoDigits = count % 100
  return lastDigit >= 2 && lastDigit <= 4 && (lastTwoDigits < 12 || lastTwoDigits > 14) ? few : many
}

export const formatPrice = (pln: number) => `${integerFormat.format(pln)} zł`

export const formatPricePerM2 = (pln: number) => `${integerFormat.format(pln)} zł/m²`

export const formatArea = (m2: number) => `${decimalFormat.format(m2)} m²`

export const formatRooms = (rooms: number) => `${rooms} ${pluralPl(rooms, 'pokój', 'pokoje', 'pokoi')}`

export const formatListingsCount = (count: number) => `${count} ${pluralPl(count, 'oferta', 'oferty', 'ofert')}`

export function formatFloor(floor: number, buildingFloors: number | undefined, higherThan10: boolean): string {
  if (floor < 0) {
    return 'suterena'
  }
  if (floor === 0) {
    return 'parter'
  }
  const label = `piętro ${floor}${higherThan10 ? '+' : ''}`
  return buildingFloors === undefined ? label : `${label}/${buildingFloors}`
}

export const formatDate = (iso: string) => new Date(iso).toLocaleDateString('pl-PL')

export function formatDetailFloor(
  floor: number | undefined,
  buildingFloors: number | undefined,
  higherThan10: boolean,
  garret: boolean,
): string | undefined {
  if (floor === undefined) {
    return garret ? 'poddasze' : undefined
  }
  if (floor < 0) {
    return 'suterena'
  }
  if (floor === 0) {
    return 'parter'
  }
  const label = `${floor}${higherThan10 ? '+' : ''}`
  return buildingFloors === undefined ? label : `${label} z ${buildingFloors}`
}

export const formatPhotosCount = (count: number) => `${count} ${pluralPl(count, 'zdjęcie', 'zdjęcia', 'zdjęć')}`

export const VERDICT_PRESENTATION: Record<string, { icon: string; label: string }> = {
  yes: { icon: '✓', label: 'pasuje' },
  partly: { icon: '◐', label: 'częściowo' },
  no: { icon: '✗', label: 'nie pasuje' },
  unknown: { icon: '?', label: 'brak danych' },
}

export const RISK_KIND_LABELS: Record<string, string> = {
  extra_cost: 'Dopłata',
  legal: 'Stan prawny',
  occupied: 'Lokal zajęty',
  noise: 'Hałas',
  field_mismatch: 'Niezgodność z danymi ogłoszenia',
}

export const ASK_GROUP_HEADINGS: Record<string, string> = {
  full: 'Pasuje',
  partial: 'Pasuje częściowo',
  near: 'Prawie pasuje: jedna rzecz nie gra',
}
