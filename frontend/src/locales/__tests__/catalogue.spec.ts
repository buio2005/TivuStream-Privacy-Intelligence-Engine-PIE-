import { describe, expect, it } from 'vitest'
import en from '@/locales/en.json'
import it_ from '@/locales/it.json'

// The catalogue is part of the product. The Network Privacy Specification
// binds every language equally: a translation that dropped a qualification
// would break the promise as surely as the engine doing so.

function keysOf(node: unknown, prefix = ''): string[] {
  if (typeof node !== 'object' || node === null) {
    return [prefix]
  }

  return Object.entries(node).flatMap(([key, value]) =>
    keysOf(value, prefix ? `${prefix}.${key}` : key),
  )
}

function lookup(catalogue: unknown, path: string): string {
  const value = path
    .split('.')
    .reduce<unknown>((node, key) => (node as Record<string, unknown> | undefined)?.[key], catalogue)

  return String(value)
}

describe('Translation catalogue', () => {
  it('holds the same entries in both languages', () => {
    // A missing entry does not fail: the interface falls back to English, and
    // a person reading Italian meets a sentence in another language without
    // being told why.
    expect(keysOf(it_).sort()).toEqual(keysOf(en).sort())
  })

  it('leaves no entry empty', () => {
    for (const catalogue of [en, it_]) {
      for (const key of keysOf(catalogue)) {
        expect(lookup(catalogue, key).trim(), key).not.toBe('')
      }
    }
  })

  it('keeps the same placeholders in both languages', () => {
    const placeholders = (text: string) => [...text.matchAll(/\{(\w+)\}/g)].map((m) => m[1]).sort()

    for (const key of keysOf(en)) {
      expect(placeholders(lookup(it_, key)), key).toEqual(placeholders(lookup(en, key)))
    }
  })
})

describe('Qualifications survive translation', () => {
  // What each sentence must keep saying, in each language.
  const mustSay: Record<string, { en: string; it: string }> = {
    'factor.TrackingExposureNone': { en: 'lower bound', it: 'limite inferiore' },
    'factor.TrackingExposureMeasured': { en: 'lower bound', it: 'limite inferiore' },
    'factor.ThreatExposureNone': { en: 'lower bound', it: 'limite inferiore' },
    'factor.ThreatExposureSuspiciousOnly': { en: 'not confirmed', it: 'non è confermata' },
    'factor.ClassificationUnavailable': {
      en: 'does not mean there is no tracking',
      it: 'non significa che non vi sia tracciamento',
    },
    'domains.unclassifiedNote': { en: 'does not mean it is safe', it: 'non significa che sia sicuro' },

    // An engine that did not answer checked nothing and may or may not have
    // changed anything. Neither sentence may turn into a verdict.
    'auth.engineUnreachable': { en: 'were not checked', it: 'non sono state verificate' },
    'error.EngineUnreachable': { en: 'no way to know', it: 'non si può sapere' },

    // What is withheld is not an absence.
    'domains.activityWithheld': { en: 'says nothing about whether', it: 'non dice se' },

    // The cost of the role is stated with it, and so is who knows a password.
    'role.note.Viewer': { en: 'does not see which device', it: 'non vede quale dispositivo' },
    'accounts.initialPasswordNote': { en: 'you know it too', it: 'la conosci anche tu' },
  }

  for (const [key, expected] of Object.entries(mustSay)) {
    it(`${key} says so in English`, () => {
      expect(lookup(en, key).toLowerCase()).toContain(expected.en)
    })

    it(`${key} says so in Italian`, () => {
      expect(lookup(it_, key).toLowerCase()).toContain(expected.it)
    })
  }
})
