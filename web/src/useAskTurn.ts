import { useQuery } from '@tanstack/react-query'
import { useState } from 'react'
import { streamAsk } from './api.ts'
import type { AskUnderstood } from './api.ts'

export function useAskTurn(history: string[], message: string, excludeNeeds: number[]) {
  const queryKey = ['ask', history, message, excludeNeeds]
  const id = JSON.stringify(queryKey)
  const [streamed, setStreamed] = useState<{ id: string; value: AskUnderstood }>()
  const sameConversation = (key: readonly unknown[]) => key[2] === message && JSON.stringify(key[1]) === JSON.stringify(history)

  const query = useQuery({
    queryKey,
    queryFn: ({ signal }) =>
      streamAsk({ message, history, excludeNeeds, more: false }, (value) => setStreamed({ id, value }), signal),
    retry: false,
    staleTime: Infinity,
    placeholderData: (previous, previousQuery) => (previousQuery && sameConversation(previousQuery.queryKey) ? previous : undefined),
  })

  return { query, queryKey, understood: streamed?.id === id ? streamed.value : undefined }
}
