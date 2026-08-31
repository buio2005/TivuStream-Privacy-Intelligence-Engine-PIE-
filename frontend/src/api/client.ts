import type { ApiResponse } from './types'

/**
 * Reads from the Privacy Intelligence Engine.
 *
 * The interface belongs to the Query Flow alone: it asks for results already
 * produced and never causes an acquisition.
 */
export async function read<T>(path: string): Promise<T> {
  const response = await fetch(`/api/v1/${path}`, {
    headers: { Accept: 'application/json' },
  })

  const body = (await response.json()) as ApiResponse<T>

  if (!body.success || body.data === null) {
    throw new Error(body.error?.code ?? 'UnknownError')
  }

  return body.data
}
