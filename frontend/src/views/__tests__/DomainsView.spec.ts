import { afterEach, describe, expect, it, vi } from 'vitest'
import { RouterLinkStub } from '@vue/test-utils'
import DomainsView from '@/views/DomainsView.vue'
import type { Domain, ObservedDomains } from '@/api/types'
import { answer, mountView, refusal } from './support'

// API Specification 1.2.0: a list of what was observed carries the interval it
// refers to, and says how much of what was asked for actually exists.

afterEach(() => {
  vi.unstubAllGlobals()
})

function domain(overrides: Partial<Domain> = {}): Domain {
  return {
    name: 'unknown.example',
    category: 'Unknown',
    categoryConfidence: null,
    categorySource: null,
    categorySourceUpdatedAt: null,
    reputation: null,
    firstSeen: '2026-09-01T10:00:00Z',
    lastSeen: '2026-09-01T11:00:00Z',
    observationQuality: 'PeriodBounded',
    occurrences: 3,
    ...overrides,
  }
}

function observed(overrides: Partial<ObservedDomains> = {}): ObservedDomains {
  return {
    period: { start: '2026-09-01T06:00:00Z', end: '2026-09-01T11:00:00Z' },
    periodsObserved: 5,
    periodsRequested: 24,
    domains: [domain()],
    ...overrides,
  }
}

describe('Domains: what was asked for and what exists are told apart', () => {
  it('declares how many hours were observed out of those requested', async () => {
    const view = await mountView(DomainsView, answer(observed()))

    // An installation running for five hours must not report a day.
    expect(view.text()).toContain('5 hours observed out of the 24 requested')
  })

  it('uses the singular for a single hour', async () => {
    const view = await mountView(DomainsView, answer(observed({ periodsObserved: 1 })))

    expect(view.text()).toContain('1 hour observed out of the 24 requested')
  })

  it('reports an empty list together with the interval it is empty of', async () => {
    const view = await mountView(DomainsView, answer(observed({ domains: [] })))

    // "Nothing observed" is a statement about the moment of looking unless
    // the interval travels with it.
    expect(view.text()).toContain('No domain observed between')
    expect(view.text()).not.toContain('No observation recorded in the last twenty-four hours')
  })

  it('tells a window without any observed hour from one with an empty period, without claiming nothing was ever observed', async () => {
    const view = await mountView(
      DomainsView,
      answer(observed({ period: null, periodsObserved: 0, domains: [] })),
    )

    expect(view.text()).toContain('No observation recorded in the last twenty-four hours')
    expect(view.text()).not.toContain('No domain observed between')
  })

  it('says the list could not be read instead of showing an empty one', async () => {
    const view = await mountView(DomainsView, refusal('InternalError'))

    // A read that failed must not look like a network that contacted nothing,
    // and must not borrow the wording of the score page.
    expect(view.text()).toContain('Domains could not be read')
    expect(view.text()).toContain('says nothing about what the network contacted')
    expect(view.text()).not.toContain('score')
    expect(view.find('.domains').exists()).toBe(false)
    expect(view.text()).not.toContain('No domain observed')
    expect(view.text()).not.toContain('No observation recorded in the last twenty-four hours')
  })
})

describe('Domains: an unrecognised domain is not a safe one', () => {
  it('labels a domain no list recognises as unclassified', async () => {
    const view = await mountView(DomainsView, answer(observed()))

    expect(view.find('.category.unclassified').text()).toBe('Unclassified')
  })

  it('attributes no list and no confidence to a domain no list recognised', async () => {
    const view = await mountView(DomainsView, answer(observed()))

    // Only the line about when it was seen remains under the name.
    expect(view.findAll('p.note:not(.observation)')).toHaveLength(0)
    expect(view.text()).not.toContain('Name present in a list')
  })

  it('names the list a classification comes from, with its confidence', async () => {
    const view = await mountView(
      DomainsView,
      answer(
        observed({
          domains: [
            domain({
              name: 'tracker.example',
              category: 'Tracking',
              categoryConfidence: 'Medium',
              categorySource: 'list-one',
              categorySourceUpdatedAt: '2026-08-30T00:00:00Z',
            }),
          ],
        }),
      ),
    )

    expect(view.find('.category').classes()).not.toContain('unclassified')
    expect(view.text()).toContain('Inferred from the parent domain')
    expect(view.text()).toContain('From list-one')
  })

  it('offers the explanation of "unclassified" once, not on every row', async () => {
    const view = await mountView(
      DomainsView,
      answer(observed({ domains: [domain({ name: 'a.example' }), domain({ name: 'b.example' })] })),
    )

    expect(view.findAll('details.term summary').filter((s) => s.text().includes('unclassified'))).toHaveLength(1)
  })
})

describe('Domains: each domain leads to its detail', () => {
  it('links the name of a domain to its own page', async () => {
    const view = await mountView(DomainsView, answer(observed({ domains: [domain({ name: 'a.example' })] })))

    const link = view.findComponent(RouterLinkStub)

    expect(link.text()).toBe('a.example')
    expect(link.props('to')).toEqual({ name: 'domain', params: { domain: 'a.example' } })
  })
})
