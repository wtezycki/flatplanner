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
