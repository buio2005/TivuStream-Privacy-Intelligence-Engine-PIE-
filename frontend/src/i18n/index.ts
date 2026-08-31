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
  numberFormats: {
    it: { percent: { style: 'percent', maximumFractionDigits: 1 } },
    en: { percent: { style: 'percent', maximumFractionDigits: 1 } },
  },
})
