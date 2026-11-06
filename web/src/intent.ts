import type { SearchIntent } from './api.ts'

export const FILTER_PARAMS = [
  'cities',
  'districts',
  'priceMin',
  'priceMax',
  'areaMin',
  'areaMax',
  'areaApprox',
  'roomsMin',
  'roomsMax',
  'floorMin',
  'floorMax',
  'market',
  'advertiserType',
  'requireLift',
  'requireBalcony',
] as const

function numberParam(searchParams: URLSearchParams, key: string): number | undefined {
  const raw = searchParams.get(key)?.trim()
  if (!raw) {
    return undefined
  }
  const value = Number(raw)
  return Number.isFinite(value) ? value : undefined
}

export function intentFromSearchParams(searchParams: URLSearchParams): SearchIntent {
  return {
    cities: searchParams.getAll('cities'),
    districts: searchParams.getAll('districts'),
    priceMin: numberParam(searchParams, 'priceMin'),
    priceMax: numberParam(searchParams, 'priceMax'),
    areaMin: numberParam(searchParams, 'areaMin'),
    areaMax: numberParam(searchParams, 'areaMax'),
    areaApprox: numberParam(searchParams, 'areaApprox'),
    roomsMin: numberParam(searchParams, 'roomsMin'),
    roomsMax: numberParam(searchParams, 'roomsMax'),
    floorMin: numberParam(searchParams, 'floorMin'),
    floorMax: numberParam(searchParams, 'floorMax'),
    market: searchParams.get('market') ?? undefined,
    advertiserType: searchParams.get('advertiserType') ?? undefined,
    requireLift: searchParams.get('requireLift') === 'true',
    requireBalcony: searchParams.get('requireBalcony') === 'true',
    text: searchParams.get('text') ?? undefined,
    preferences: searchParams.getAll('preferences'),
    requestedCityOutOfScope: searchParams.get('requestedCityOutOfScope') ?? undefined,
    outOfScope: searchParams.getAll('outOfScope'),
  }
}

export function clearFilters(searchParams: URLSearchParams): URLSearchParams {
  const next = new URLSearchParams(searchParams)
  for (const param of FILTER_PARAMS) {
    next.delete(param)
  }
  next.delete('page')
  return next
}

export function activeFilterCount(intent: SearchIntent): number {
  const groups = [
    intent.cities.length > 0,
    intent.districts.length > 0,
    intent.priceMin !== undefined || intent.priceMax !== undefined,
    intent.areaMin !== undefined || intent.areaMax !== undefined || intent.areaApprox !== undefined,
    intent.roomsMin !== undefined || intent.roomsMax !== undefined,
    intent.floorMin !== undefined || intent.floorMax !== undefined,
    intent.market !== undefined,
    intent.advertiserType !== undefined,
    intent.requireLift,
    intent.requireBalcony,
  ]
  return groups.filter(Boolean).length
}

const WORKSPACE_PARAMS = ['q', 'view', 'listing'] as const

export function withoutWorkspaceParams(searchParams: URLSearchParams): URLSearchParams {
  const next = new URLSearchParams(searchParams)
  for (const param of WORKSPACE_PARAMS) {
    next.delete(param)
  }
  return next
}

export function listingSearch(searchParams: URLSearchParams, id: number): string {
  const next = new URLSearchParams(searchParams)
  next.set('listing', String(id))
  return next.toString()
}
