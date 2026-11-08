import { useQuery } from '@tanstack/react-query'
import { metaQuery } from './api.ts'
import type { EnumOption, ListingItem } from './api.ts'

const labelOf = (options: EnumOption[] | undefined, value: string | undefined) =>
  options?.find((option) => option.value === value)?.label

export interface CardLabels {
  market?: string
  finishCondition?: string
  ownership?: string
}

export function useCardLabels(): (item: ListingItem) => CardLabels {
  const enums = useQuery(metaQuery).data?.enums
  return (item) => ({
    market: labelOf(enums?.market, item.market),
    finishCondition: labelOf(enums?.finishCondition, item.finishCondition),
    ownership: labelOf(enums?.ownership, item.ownership),
  })
}
