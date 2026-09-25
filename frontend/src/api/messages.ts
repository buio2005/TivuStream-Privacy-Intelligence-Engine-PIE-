import { EngineUnreachable } from './client'

/** An entry of the catalogue, with the values it needs. */
export interface Message {
  key: string
  params?: Record<string, unknown>
}

/**
 * Where a refusal was met.
 *
 * `credentials` is a sign in or a setup: nothing was changed, and an engine
 * that did not answer did not check anything. `change` is anything else: an
 * engine that did not answer may or may not have applied the change, and
 * saying either would be a claim nobody can support.
 */
export type RefusalContext = 'credentials' | 'change'

const byCode: Record<string, string> = {
  AuthenticationFailed: 'auth.failed',
  TransportNotSecure: 'auth.notSecure',
  SetupCodeRejected: 'setup.codeRejected',
  SetupAlreadyCompleted: 'setup.alreadyCompleted',
  CurrentPasswordRejected: 'password.currentRejected',
  UsernameRejected: 'error.UsernameRejected',
  RoleRejected: 'error.RoleRejected',
  AccountExists: 'error.AccountExists',
  AccountNotFound: 'error.AccountNotFound',
  LastAdministrator: 'error.LastAdministrator',
  Forbidden: 'error.Forbidden',
  OriginNotAllowed: 'error.OriginNotAllowed',
}

const reasons = new Set(['TooShort', 'TooLong', 'EqualsUsername', 'Unchanged'])

/**
 * The words for a refusal of the engine.
 *
 * Only the code and the reason decide them, never the engine's own message:
 * the engine speaks English to whoever reads its logs, and a sign in refused
 * for any reason must read the same whatever the engine said.
 */
export function describeRefusal(
  refusal: { code: string; reason?: string; retryAfter?: number },
  context: RefusalContext,
): Message {
  if (refusal.code === EngineUnreachable) {
    return { key: context === 'credentials' ? 'auth.engineUnreachable' : 'error.EngineUnreachable' }
  }

  if (refusal.code === 'TooManyAttempts') {
    return { key: 'auth.tooManyAttempts', params: { seconds: refusal.retryAfter ?? '?' } }
  }

  if (refusal.code === 'PasswordRejected' && refusal.reason && reasons.has(refusal.reason)) {
    return { key: `reason.${refusal.reason}` }
  }

  const key = byCode[refusal.code]

  // A code this interface does not know is shown as such, never dropped or
  // replaced by a guess.
  return key ? { key } : { key: 'error.unexpected', params: { code: refusal.code } }
}
