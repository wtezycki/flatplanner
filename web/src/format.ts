import type { IconName } from './components/Icon.tsx'

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

const NEW_LISTING_DAYS = 7
const DAY_MS = 24 * 60 * 60 * 1000

export const isNewListing = (createdAtSource: string) =>
  Date.now() - new Date(createdAtSource).getTime() < NEW_LISTING_DAYS * DAY_MS

export const formatBelowMedian = (percent: number) => `${percent}% poniżej mediany miasta`

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

const VERDICT_PRESENTATION: Record<string, { icon: IconName; label: string }> = {
  yes: { icon: 'check', label: 'pasuje' },
  partly: { icon: 'halfCircle', label: 'częściowo' },
  no: { icon: 'cross', label: 'nie pasuje' },
  unknown: { icon: 'help', label: 'brak danych' },
}

export function verdictPresentation(verdict: string): { icon: IconName; label: string } {
  return VERDICT_PRESENTATION[verdict] ?? VERDICT_PRESENTATION.unknown
}

export const RISK_KIND_LABELS: Record<string, string> = {
  extra_cost: 'Dopłata',
  legal: 'Stan prawny',
  occupied: 'Lokal zajęty',
  noise: 'Hałas',
  field_mismatch: 'Niezgodność z danymi ogłoszenia',
}

export const RISK_KIND_HINTS: Record<string, string> = {
  extra_cost: 'Opis wspomina o koszcie ponad cenę mieszkania, np. miejsce postojowe, garaż lub komórka za dopłatą.',
  legal: 'Opis wskazuje na kwestię prawną, np. sprzedaż z VAT, syndyk, garaż w osobnej księdze wieczystej albo kredyt do spłaty z ceny.',
  occupied: 'Opis wskazuje, że lokal jest wynajęty lub zajęty.',
  noise: 'Opis wymienia źródło hałasu, np. klub, główną ulicę albo planowaną linię tramwajową.',
  field_mismatch: 'Opis przeczy jednemu z pól ogłoszenia, np. liczbie pokoi, piętru albo czynszowi. Dopytaj sprzedającego.',
}

export const PRICE_OUTLIER_HINT = 'Cena za m² poza zakresem 7 000–40 000 zł. Może to być błąd w ogłoszeniu, sprawdź opis.'

export const RISK_SOURCE_NOTE =
  'Wykryte przez AI w opisie ogłoszenia. Każdy cytat sprawdziliśmy dosłownie w tekście. Zweryfikuj to ze sprzedającym.'

export const ASK_GROUP_HEADINGS: Record<string, string> = {
  full: 'Pasuje',
  partial: 'Pasuje częściowo',
  near: 'Prawie pasuje: jedna rzecz nie gra',
}

export const ASK_WHY_LABELS: Record<string, string> = {
  full: 'Dlaczego pasuje',
  partial: 'Dlaczego pasuje częściowo',
  near: 'Co nie pasuje',
}

export const ASK_GROUP_HINTS: Record<string, string> = {
  full: 'Spełnia wszystkie Twoje warunki',
  partial: 'Część warunków wymaga potwierdzenia',
  near: 'Warto rzucić okiem, jeśli odpuścisz jeden warunek',
}
