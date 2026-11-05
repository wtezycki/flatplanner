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
  requireLift: boolean
  requireBalcony: boolean
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
  imageUrl?: string
  market: string
  createdAtSource: string
  locationApproximate: boolean
  priceOutlier: boolean
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

async function getJson<T>(url: string, signal?: AbortSignal): Promise<T> {
  const response = await fetch(url, { signal })
  if (!response.ok) {
    const retryAfter = Number.parseInt(response.headers.get('Retry-After') ?? '', 10)
    throw new ApiError(response.status, Number.isNaN(retryAfter) ? undefined : retryAfter)
  }
  return (await response.json()) as T
}

export function fetchListings(searchParams: URLSearchParams, signal?: AbortSignal): Promise<ListingsResponse> {
  return getJson(`/api/listings?${searchParams.toString()}`, signal)
}

export const metaQuery = queryOptions({
  queryKey: ['meta'],
  queryFn: ({ signal }) => getJson<MetaResponse>('/api/meta', signal),
})
