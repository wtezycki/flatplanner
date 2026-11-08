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
  finishConditions: string[]
  ownerships: string[]
  buildingTypes: string[]
  heatings: string[]
  buildingMaterials: string[]
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
  chatMaxTurns: number
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
  priceOutlier: boolean
  risks: ListingRisk[]
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
  listing: ListingItem
  group: string
  needs: NeedResult[]
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

export const OFF_TOPIC_NOTICE = 'off_topic'

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

function errorFrom(response: Response): ApiError {
  const retryAfter = Number.parseInt(response.headers.get('Retry-After') ?? '', 10)
  return new ApiError(response.status, Number.isNaN(retryAfter) ? undefined : retryAfter)
}

async function requestJson<T>(url: string, init: RequestInit): Promise<T> {
  const response = await fetch(url, init)
  if (!response.ok) {
    throw errorFrom(response)
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

export interface AskUnderstood {
  summary: string
  needs: string[]
  declined: DeclinedNeed[]
  notices: AskNotice[]
}

export interface AskStreamRequest {
  message: string
  history: string[]
  excludeNeeds: number[]
  more: boolean
}

function parseEvent(block: string): { name: string; data: string } | undefined {
  let name = ''
  let data = ''
  for (const line of block.split('\n')) {
    if (line.startsWith('event: ')) {
      name = line.slice('event: '.length)
    } else if (line.startsWith('data: ')) {
      data += line.slice('data: '.length)
    }
  }
  return name && data ? { name, data } : undefined
}

export async function streamAsk(
  request: AskStreamRequest,
  onUnderstood: ((understood: AskUnderstood) => void) | undefined,
): Promise<AskResponse> {
  const response = await fetch('/api/search/ask/stream', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(request),
  })
  if (!response.ok || !response.body) {
    throw errorFrom(response)
  }

  const reader = response.body.getReader()
  const decoder = new TextDecoder()
  let buffer = ''
  try {
    for (;;) {
      const { done, value } = await reader.read()
      buffer += decoder.decode(value, { stream: !done })
      const blocks = buffer.split('\n\n')
      buffer = blocks.pop() ?? ''
      for (const block of blocks) {
        const event = parseEvent(block)
        if (event?.name === 'understood') {
          onUnderstood?.(JSON.parse(event.data) as AskUnderstood)
        } else if (event?.name === 'results') {
          return JSON.parse(event.data) as AskResponse
        } else if (event?.name === 'error') {
          throw new ApiError(500)
        }
      }
      if (done) {
        throw new ApiError(500)
      }
    }
  } finally {
    void reader.cancel()
  }
}

export const metaQuery = queryOptions({
  queryKey: ['meta'],
  queryFn: ({ signal }) => getJson<MetaResponse>('/api/meta', signal),
})

// gcTime stays at the default 5 minutes, like the cached answers: a left conversation and its answers expire together.
export const threadQuery = queryOptions({
  queryKey: ['thread'],
  queryFn: (): string[] => [],
  initialData: [] as string[],
  staleTime: Infinity,
})
