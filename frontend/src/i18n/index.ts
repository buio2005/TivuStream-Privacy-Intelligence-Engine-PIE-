import { createI18n } from 'vue-i18n'
import en from '@/locales/en.json'
import it from '@/locales/it.json'

export const supportedLocales = ['it', 'en'] as const

export type SupportedLocale = (typeof supportedLocales)[number]

/**
 * Chooses the locale, preferring what the person already asked for.
 */
function resolveLocale(): SupportedLocale {
  const stored = localStorage.getItem('locale')

  if (stored && supportedLocales.includes(stored as SupportedLocale)) {
    return stored as SupportedLocale
  }

  return navigator.language.startsWith('it') ? 'it' : 'en'
}

export const i18n = createI18n({
  legacy: false,
  locale: resolveLocale(),
  fallbackLocale: 'en',
  messages: { it, en },
  datetimeFormats: {
    it: {
      short: { hour: '2-digit', minute: '2-digit' },
      stamp: { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' },
      day: { day: 'numeric', month: 'long', year: 'numeric' },
    },
    en: {
      short: { hour: '2-digit', minute: '2-digit' },
      stamp: { day: 'numeric', month: 'short', hour: '2-digit', minute: '2-digit' },
      day: { day: 'numeric', month: 'long', year: 'numeric' },
    },
  },
  numberFormats: {
    it: { percent: { style: 'percent', maximumFractionDigits: 1 } },
    en: { percent: { style: 'percent', maximumFractionDigits: 1 } },
  },
})
