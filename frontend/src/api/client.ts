import type { ApiResponse } from './types'

/**
 * What a request to the engine came to.
 *
 * A refusal and an engine that did not answer are different facts. The first
 * carries the engine's code; the second is `EngineUnreachable`, and nothing
 * may be concluded from it about what was asked: not that the credentials
 * were wrong, not that a change was or was not applied.
 */
export type Outcome<T> =
  | { ok: true; data: T }
  | { ok: false; code: string; reason?: string; retryAfter?: number }

/** Code of an outcome in which the engine gave no answer that could be read. */
export const EngineUnreachable = 'EngineUnreachable'

type RefusalListener = (code: string) => void

let refusalListener: RefusalListener | null = null

/**
 * Registers who is told when the engine refuses a request because of the
 * session: it ended, or its password must be changed first.
 *
 * These can arrive in answer to any request, from any screen. Deciding what
 * they mean for the whole interface belongs in one place.
 */
export function onSessionRefusal(listener: RefusalListener | null) {
  refusalListener = listener
}

const sessionRefusals = new Set(['AuthenticationRequired', 'PasswordChangeRequired'])

/**
 * Sends a request to the Privacy Intelligence Engine.
 *
 * The session travels in a cookie the page cannot read. Nothing here holds a
 * credential beyond the single request that carries it.
 */
export async function call<T>(method: string, path: string, body?: unknown): Promise<Outcome<T>> {
  let response: Response
  let answer: ApiResponse<T>

  try {
    response = await fetch(`/api/v1/${path}`, {
      method,
      headers:
        body === undefined
          ? { Accept: 'application/json' }
          : { Accept: 'application/json', 'Content-Type': 'application/json' },
      body: body === undefined ? undefined : JSON.stringify(body),
    })

    answer = (await response.json()) as ApiResponse<T>
  } catch {
    // No answer, or one that is not the engine's: a proxy's error page, a
    // connection dropped half way.
    return { ok: false, code: EngineUnreachable }
  }

  if (answer?.success === true) {
    return { ok: true, data: answer.data as T }
  }

  const code = answer?.error?.code ?? EngineUnreachable

  if (sessionRefusals.has(code)) {
    refusalListener?.(code)
  }

  const retryAfter = Number(response.headers?.get('Retry-After'))

  return {
    ok: false,
    code,
    reason: answer?.error?.reason,
    retryAfter: Number.isFinite(retryAfter) && retryAfter > 0 ? retryAfter : undefined,
  }
}

/**
 * Reads from the Privacy Intelligence Engine.
 *
 * The interface belongs to the Query Flow alone: it asks for results already
 * produced and never causes an acquisition.
 */
export async function read<T>(path: string): Promise<T> {
  const outcome = await call<T>('GET', path)

  if (!outcome.ok || outcome.data === null) {
    throw new Error(outcome.ok ? 'UnknownError' : outcome.code)
  }

  return outcome.data
}
