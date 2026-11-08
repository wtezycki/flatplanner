import { queryOptions } from '@tanstack/react-query'

export interface SearchIntent {
  cities: string[]
  districts: string[]
  priceMin?: number
  priceMax?: number
  areaMin?: number
  areaMax?: number
  areaApprox?: number
  roomsMin?: number
  roomsMax?: number
  floorMin?: number
  floorMax?: number
  market?: string
  advertiserType?: string
  buildYearMin?: number
  buildYearMax?: number
  pricePerM2Min?: number
  pricePerM2Max?: number
  rentMin?: number
  rentMax?: number
  finishCondition?: string
  ownership?: string
  buildingType?: string
  heating?: string
  buildingMaterial?: string
  requireLift: boolean
  requireBalcony: boolean
  requireGarage: boolean
  requireBasement: boolean
  text?: string
  preferences: string[]
  requestedCityOutOfScope?: string
  outOfScope: string[]
}

export interface EnumOption {
  value: string
  label: string
}

export interface DistrictMeta {
  key: string
  name: string
  count: number
}

export interface CityMeta {
  slug: string
  name: string
  count: number
  districts: DistrictMeta[]
}

export interface MetaResponse {
  cities: CityMeta[]
  defaultCitySlug: string
  enums: {
    market: EnumOption[]
    advertiserType: EnumOption[]
    finishCondition: EnumOption[]
    ownership: EnumOption[]
    heating: EnumOption[]
    buildingType: EnumOption[]
    windowsType: EnumOption[]
    buildingMaterial: EnumOption[]
    factStatus: EnumOption[]
    sort: EnumOption[]
    provenance: EnumOption[]
  }
  chatMode: string
  chatMaxLength: number
  dataAsOf?: string
}

export interface ListingItem {
  id: number
  title: string
  pricePln: number
  areaM2: number
  pricePerM2: number
  rooms: number
  floor?: number
  floorHigherThan10: boolean
  buildingFloors?: number
  citySlug: string
  cityName: string
  districtName?: string
  imageUrls: string[]
  market: string
  createdAtSource: string
  locationApproximate: boolean
  priceOutlier: boolean
  buildYear?: number
  finishCondition?: string
  ownership?: string
  hasLift?: boolean
  hasBalcony?: boolean
  hasGarage?: boolean
  hasBasement?: boolean
  riskKinds: string[]
  nearbyStop?: string
  nearbyPark?: string
  belowMedianPercent?: number
}

export interface SearchWarning {
  field: string
  message: string
}

export interface Chip {
  key: string
  label: string
}

export interface Interpretation {
  summary: string
  chips: Chip[]
  warnings: SearchWarning[]
}

export interface ListingsResponse {
  items: ListingItem[]
  page: number
  pageSize: number
  total: number
  totalPages: number
  sort: string
  interpretation: Interpretation
}

export interface ListingDetail {
  id: number
  url: string
  title: string
  scrapedAt: string
  createdAtSource: string
  imageUrls: string[]
  descriptionText: string
  descriptionMarkup?: string
  citySlug: string
  cityName: string
  districtName?: string
  street?: string
  lat: number
  lng: number
  locationApproximate: boolean
  pricePln: number
  areaM2: number
  pricePerM2: number
  rooms: number
  floor?: number
  floorHigherThan10: boolean
  floorIsGarret: boolean
  buildingFloors?: number
  buildYear?: number
  market: string
  finishCondition?: string
  ownership?: string
  heating?: string
  buildingType?: string
  windowsType?: string
  buildingMaterial?: string
  rentPln?: number
  hasLift?: boolean
  hasBalcony?: boolean
  hasGarage?: boolean
  hasBasement?: boolean
  advertiserType?: string
}

export interface Evidence {
  kind: string
  label: string
}

export interface NeedResult {
  verdict: string
  reason?: string
  evidence: Evidence[]
}

export interface ListingRisk {
  kind: string
  reason: string
  quote: string
  amountPln?: number
}

export interface AskItem {
  id: number
  title: string
  pricePln: number
  areaM2: number
  rooms: number
  floor?: number
  buildingFloors?: number
  cityName: string
  districtName?: string
  imageUrl?: string
  aiSummary?: string
  locationApproximate: boolean
  group: string
  needs: NeedResult[]
  risks: ListingRisk[]
  totalWithExtrasPln?: number
}

export interface DeclinedNeed {
  text: string
  reason: string
}

export interface AskNotice {
  code: string
  text: string
}

export interface Relaxation {
  need: string
  count: number
  bestId?: number
}

export interface AskResponse {
  summary: string
  needs: string[]
  declined: DeclinedNeed[]
  notices: AskNotice[]
  items: AskItem[]
  unknownCount: number
  remainingCount: number
  relaxations: Relaxation[]
  usedFallback: boolean
  fallbackReason?: string
}

export const DEFAULT_RETRY_AFTER_SECONDS = 10

export class ApiError extends Error {
  readonly status: number
  readonly retryAfter?: number

  constructor(status: number, retryAfter?: number) {
    super(`API responded with ${status}`)
    this.status = status
    this.retryAfter = retryAfter
  }
}

export function isServerOrNetworkError(error: unknown): boolean {
  return !(error instanceof ApiError) || error.status >= 500
}

async function requestJson<T>(url: string, init: RequestInit): Promise<T> {
  const response = await fetch(url, init)
  if (!response.ok) {
    const retryAfter = Number.parseInt(response.headers.get('Retry-After') ?? '', 10)
    throw new ApiError(response.status, Number.isNaN(retryAfter) ? undefined : retryAfter)
  }
  return (await response.json()) as T
}

const getJson = <T>(url: string, signal?: AbortSignal) => requestJson<T>(url, { signal })

export function fetchListings(searchParams: URLSearchParams, signal?: AbortSignal): Promise<ListingsResponse> {
  return getJson(`/api/listings?${searchParams.toString()}`, signal)
}

export function fetchListing(id: string, signal?: AbortSignal): Promise<ListingDetail> {
  return getJson(`/api/listings/${encodeURIComponent(id)}`, signal)
}

export function postAsk(
  message: string,
  excludeNeeds: number[],
  filters: string,
  more: boolean,
  signal?: AbortSignal,
): Promise<AskResponse> {
  return requestJson(filters ? `/api/search/ask?${filters}` : '/api/search/ask', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ message, excludeNeeds, more }),
    signal,
  })
}

export const metaQuery = queryOptions({
  queryKey: ['meta'],
  queryFn: ({ signal }) => getJson<MetaResponse>('/api/meta', signal),
})
