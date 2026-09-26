import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, RouterLinkStub } from '@vue/test-utils'
import { createPinia } from 'pinia'
import DomainDetailView from '@/views/DomainDetailView.vue'
import { i18n } from '@/i18n'
import type { DomainDetail, ObservedActivity } from '@/api/types'
import { useDomainDetailStore } from '@/stores/domainDetail'
import { answer, mountView, refusal } from './support'

// Frontend Specification, Domain Detail; API Specification 1.4.0,
// /domains/{domain}; Authentication Specification, F4: what is withheld is
// not an absence.

afterEach(() => {
  vi.unstubAllGlobals()
})

function activity(overrides: Partial<ObservedActivity> = {}): ObservedActivity {
  return {
    device: {
      deviceId: '0f1e2d3c-4b5a-6978-8796-a5b4c3d2e1f0',
      hostname: 'laptop-maria',
      ipAddress: '192.168.1.20',
      identityBasis: 'HardwareAddress',
    },
    queryCount: 12,
    blocked: false,
    protocol: 'Udp',
    firstSeen: '2026-09-01T08:00:00Z',
    lastSeen: '2026-09-01T11:00:00Z',
    observationQuality: 'PeriodBounded',
    ...overrides,
  }
}

function detail(overrides: Partial<DomainDetail> = {}): DomainDetail {
  return {
    period: { start: '2026-09-01T06:00:00Z', end: '2026-09-01T11:00:00Z' },
    periodsObserved: 5,
    periodsRequested: 24,
    domain: {
      name: 'tracker.example',
      category: 'Tracking',
      categoryConfidence: 'High',
      categorySource: 'list-one',
      categorySourceUpdatedAt: '2026-08-30T00:00:00Z',
      reputation: null,
      firstSeen: '2026-09-01T08:00:00Z',
      lastSeen: '2026-09-01T11:00:00Z',
      observationQuality: 'PeriodBounded',
      occurrences: 12,
    },
    activities: [activity()],
    activityAccess: 'Available',
    ...overrides,
  }
}

function show(body: Parameters<typeof mountView>[1], locale: 'en' | 'it' = 'en') {
  return mountView(DomainDetailView, body, locale, { domain: 'tracker.example' })
}

describe('Domain detail: what the domain is', () => {
  it('asks the engine for the domain named in the address', async () => {
    await show(answer(detail()))

    expect(vi.mocked(fetch).mock.calls[0]![0]).toBe('/api/v1/domains/tracker.example')
  })

  it('declares the interval with the same sentences as the list', async () => {
    const view = await show(answer(detail()))

    expect(view.text()).toContain('5 hours observed out of the 24 requested')
  })

  it('states the classification, its list and the queries over the interval', async () => {
    const view = await show(answer(detail()))

    expect(view.find('.category').text()).toBe('Tracking')
    expect(view.text()).toContain('Name present in a list')
    expect(view.text()).toContain('From list-one')
    expect(view.text()).toContain('12 queries')
  })

  it('keeps an unclassified domain unclassified', async () => {
    const unknown = detail()
    unknown.domain = { ...unknown.domain, category: 'Unknown', categoryConfidence: null, categorySource: null, categorySourceUpdatedAt: null }

    const view = await show(answer(unknown))

    expect(view.find('.category.unclassified').text()).toBe('Unclassified')
    expect(view.text()).not.toContain('Name present in a list')
  })

  it('says the reputation is not assessed as a statement about the system', async () => {
    const view = await show(answer(detail()))

    expect(view.find('.reputation').text()).toContain('not a judgement on the domain')
  })

  it('leads back to the list', async () => {
    const view = await show(answer(detail()))

    expect(view.findComponent(RouterLinkStub).props('to')).toEqual({ name: 'domains' })
  })
})

describe('Domain detail: activity by device', () => {
  it('names the device with its address, and states the outcome and the transport', async () => {
    const view = await show(
      answer(
        detail({
          activities: [
            activity(),
            activity({ blocked: true, protocol: 'Https', queryCount: 3 }),
          ],
        }),
      ),
    )

    const rows = view.findAll('.activities tbody tr')

    expect(rows).toHaveLength(2)
    expect(rows[0]!.text()).toContain('laptop-maria (192.168.1.20)')
    expect(rows[0]!.find('.outcome').text()).toBe('Not blocked')
    expect(rows[0]!.find('.transport').text()).toBe('UDP')
    expect(rows[1]!.find('.outcome').text()).toBe('Blocked')
    expect(rows[1]!.find('.transport').text()).toBe('DNS over HTTPS')
  })

  it('never calls an answer that was not blocked "resolved"', async () => {
    const view = await show(answer(detail()), 'it')

    expect(view.find('.outcome').text()).toBe('Non bloccato')
    expect(view.text().toLowerCase()).not.toContain('risolto')
  })

  it('declares an identity founded on the network address, and only that one', async () => {
    const view = await show(
      answer(
        detail({
          activities: [
            activity({ device: { deviceId: 'aaaaaaaa-0000', hostname: null, ipAddress: '10.0.0.5', identityBasis: 'NetworkAddress' } }),
            activity(),
          ],
        }),
      ),
    )

    const rows = view.findAll('.activities tbody tr')

    expect(rows[0]!.text()).toContain('10.0.0.5')
    expect(rows[0]!.find('.identity').text()).toContain('Recognised by network address')
    expect(rows[1]!.find('.identity').exists()).toBe(false)
  })

  it('shows an undescribed device by the start of its identifier, and says only that is known', async () => {
    const view = await show(
      answer(
        detail({
          activities: [
            activity({ device: { deviceId: 'aaaaaaaa-1111', hostname: null, ipAddress: null, identityBasis: null } }),
            activity({ device: { deviceId: 'bbbbbbbb-2222', hostname: null, ipAddress: null, identityBasis: null } }),
          ],
        }),
      ),
    )

    const rows = view.findAll('.activities tbody tr')

    // Two devices nobody described stay two.
    expect(rows[0]!.find('.device').text()).toBe('Device aaaaaaaa')
    expect(rows[1]!.find('.device').text()).toBe('Device bbbbbbbb')
    expect(rows[0]!.find('.identity').text()).toContain('only its identifier is known')
  })

  it('shows a transport the catalogue does not know as the source names it, and an empty one as not stated', async () => {
    const view = await show(
      answer(detail({ activities: [activity({ protocol: 'Doh3' }), activity({ protocol: '', blocked: true })] })),
    )

    const transports = view.findAll('.transport').map((cell) => cell.text())

    expect(transports).toEqual(['Doh3', 'Not stated'])
  })

  it('does not add the rows of a device into a total of its own', async () => {
    const view = await show(
      answer(detail({ activities: [activity({ queryCount: 9 }), activity({ queryCount: 3, blocked: true })] })),
    )

    // The domain states 12; no cell may state it again as the device's sum.
    expect(view.findAll('.activities td').map((cell) => cell.text())).not.toContain('12')
  })

  it('says the source records no activity for the domain, rather than showing an empty table', async () => {
    const view = await show(answer(detail({ activities: [] })))

    expect(view.find('.activities').exists()).toBe(false)
    expect(view.find('.activity-note').text()).toContain('what is missing is the detail, not the traffic')
  })

  it('says the source does not offer the activity', async () => {
    const view = await show(answer(detail({ activities: [], activityAccess: 'Unavailable' })))

    expect(view.find('.activities').exists()).toBe(false)
    expect(view.find('.activity-note').text()).toContain('does not record which device')
    expect(view.text()).not.toContain('visible to administrators')
  })

  it('F4: shows withheld activity as withheld, not as an absence and not as an error', async () => {
    const view = await show(answer(detail({ activities: [], activityAccess: 'Withheld' })))

    expect(view.find('.activities').exists()).toBe(false)
    expect(view.find('.activity-note').text()).toContain('visible to administrators only')
    expect(view.find('.activity-note').text()).toContain('says nothing about whether')
    expect(view.text()).not.toContain('No activity by device recorded')
    expect(view.text()).not.toContain('Unable to read')
  })
})

describe('Domain detail: when there is no detail', () => {
  it('says the domain was not observed in the interval', async () => {
    const view = await show(refusal('DomainNotObserved'))

    expect(view.find('.not-observed').text()).toBe(
      'This domain is not among those observed in the last twenty-four hours.',
    )
    expect(view.text()).not.toContain('Unable to read')
    expect(view.findComponent(RouterLinkStub).exists()).toBe(true)
  })

  it('says a failed read says nothing about the network, and does not claim the domain is absent', async () => {
    const view = await show(refusal('InternalError'))

    expect(view.find('.unavailable').text()).toContain('Unable to read the domain detail')
    expect(view.find('.unavailable').text()).toContain('says nothing about what the network contacted')
    expect(view.text()).not.toContain('not among those observed')
  })

  it('treats an engine that did not answer as a failed read', async () => {
    i18n.global.locale.value = 'en'
    vi.stubGlobal('fetch', vi.fn(() => Promise.reject(new TypeError('Failed to fetch'))))

    const view = mount(DomainDetailView, {
      props: { domain: 'tracker.example' },
      global: { plugins: [createPinia(), i18n], stubs: { RouterLink: RouterLinkStub } },
    })

    await flushPromises()

    expect(view.find('.unavailable').exists()).toBe(true)
  })
})

describe('Domain detail: moving from one domain to another', () => {
  it('does not leave the previous domain on screen while the next one is read', async () => {
    i18n.global.locale.value = 'en'

    vi.stubGlobal(
      'fetch',
      vi.fn((url: string) =>
        url.endsWith('first.example')
          ? Promise.resolve({ json: () => Promise.resolve(answer(detail())) })
          : new Promise(() => {}),
      ),
    )

    const view = mount(DomainDetailView, {
      props: { domain: 'first.example' },
      global: { plugins: [createPinia(), i18n], stubs: { RouterLink: RouterLinkStub } },
    })

    await flushPromises()

    expect(view.text()).toContain('laptop-maria')

    await view.setProps({ domain: 'second.example' })
    await flushPromises()

    expect(view.text()).not.toContain('laptop-maria')
    expect(view.text()).toContain('Reading')

    // Not merely hidden by the page: the store no longer holds it.
    expect(useDomainDetailStore().detail).toBeNull()
  })

  it('drops an answer about the previous domain that arrives late', async () => {
    i18n.global.locale.value = 'en'

    let answerFirst: (value: unknown) => void = () => {}

    const second = detail({ activities: [activity({ device: { ...activity().device, hostname: 'phone-luca' } })] })

    vi.stubGlobal(
      'fetch',
      vi.fn((url: string) =>
        url.endsWith('first.example')
          ? new Promise((resolve) => {
              answerFirst = resolve
            })
          : Promise.resolve({ json: () => Promise.resolve(answer(second)) }),
      ),
    )

    const view = mount(DomainDetailView, {
      props: { domain: 'first.example' },
      global: { plugins: [createPinia(), i18n], stubs: { RouterLink: RouterLinkStub } },
    })

    await flushPromises()

    await view.setProps({ domain: 'second.example' })
    await flushPromises()

    expect(view.text()).toContain('phone-luca')

    // The first answer arrives after the second: it is about another domain.
    answerFirst({ json: () => Promise.resolve(answer(detail())) })
    await flushPromises()

    expect(view.text()).toContain('phone-luca')
    expect(view.text()).not.toContain('laptop-maria')
  })
})
