import { afterEach, describe, expect, it, vi } from 'vitest'
import DashboardView from '@/views/DashboardView.vue'
import type { Npss, ScoreComponent, ScoreFactor } from '@/api/types'
import { answer, mountView, refusal } from './support'

// Frontend Specification: the interface renders facts in words, and the
// rules of the Network Privacy Specification apply to those words in every
// language offered.

afterEach(() => {
  vi.unstubAllGlobals()
})

function area(
  component: string,
  factors: ScoreFactor[] = [],
  state: ScoreComponent['state'] = 'Measured',
): ScoreComponent {
  return { component, state, score: 12.5, maxScore: 25, weight: 25, factors }
}

function score(overrides: Partial<Npss> = {}): Npss {
  return {
    overallScore: 62,
    status: 'Fair',
    trend: null,
    coverage: 85,
    algorithmVersion: '1.0',
    generatedAt: '2026-09-01T12:00:00Z',
    breakdown: [area('DnsSecurity')],
    ...overrides,
  }
}

describe('Dashboard: three states that must not be mistaken for one another', () => {
  it('declines to judge when coverage is below the minimum, and says it did so', async () => {
    const view = await mountView(
      DashboardView,
      answer(
        score({
          overallScore: null,
          status: null,
          coverage: 40,
          breakdown: [area('DnsSecurity', [{ code: 'DnssecValidationEnabled', values: {} }])],
        }),
      ),
    )

    expect(view.find('.withheld').exists()).toBe(true)
    expect(view.text()).toContain('No overall score produced')
    expect(view.text()).toContain('Coverage is 40 out of 100')
  })

  it('shows no number at all when the score was withheld, least of all a zero', async () => {
    const view = await mountView(
      DashboardView,
      answer(score({ overallScore: null, status: null, coverage: 40 })),
    )

    expect(view.find('.overall').exists()).toBe(false)
    expect(view.text()).not.toContain('/ 100')
  })

  it('still reports the areas that were measured when the overall score is withheld', async () => {
    const view = await mountView(
      DashboardView,
      answer(
        score({
          overallScore: null,
          status: null,
          coverage: 40,
          breakdown: [area('DnsSecurity', [{ code: 'DnssecValidationEnabled', values: {} }])],
        }),
      ),
    )

    expect(view.text()).toContain('DNS security')
    expect(view.text()).toContain('DNSSEC validation is on.')
  })

  it('does not present a withheld score as a failure', async () => {
    const view = await mountView(
      DashboardView,
      answer(score({ overallScore: null, status: null, coverage: 40 })),
    )

    expect(view.text()).not.toContain('No score available')
  })

  it('presents a failure to obtain a score as unavailable, not as a refusal to judge', async () => {
    const view = await mountView(DashboardView, refusal('ScorePending'))

    expect(view.text()).toContain('No score available')
    expect(view.find('.withheld').exists()).toBe(false)
    expect(view.find('.overall').exists()).toBe(false)
  })

  it('says it is reading while the answer has not arrived, and nothing else', async () => {
    const view = await mountView(DashboardView, null)

    expect(view.text()).toContain('Reading')
    expect(view.text()).not.toContain('No score available')
    expect(view.find('.withheld').exists()).toBe(false)
    expect(view.find('.overall').exists()).toBe(false)
  })

  it('shows the number when there is one', async () => {
    const view = await mountView(DashboardView, answer(score({ overallScore: 62 })))

    expect(view.find('.overall .value').text()).toBe('62')
    expect(view.find('.withheld').exists()).toBe(false)
  })
})

describe('Dashboard: every reason is shown, and shown in the language being read', () => {
  it('shows a factor the catalogue does not know by its identifier rather than dropping it', async () => {
    const view = await mountView(
      DashboardView,
      answer(score({ breakdown: [area('DnsSecurity', [{ code: 'ReasonAddedByANewerEngine', values: {} }])] })),
    )

    // A factor that vanished would remove a reason for the score without
    // saying so.
    expect(view.text()).toContain('ReasonAddedByANewerEngine')
  })

  it('writes a share the way the language writes it, the engine having sent a fraction', async () => {
    const body = answer(
      score({
        breakdown: [
          area('PrivacyProtection', [{ code: 'TrackingExposureMeasured', values: { share: 0.071 } }]),
        ],
      }),
    )

    const english = await mountView(DashboardView, body, 'en')
    expect(english.text()).toContain('7.1%')

    const italian = await mountView(DashboardView, body, 'it')
    expect(italian.text()).toContain('7,1%')
  })

  it('offers the explanation of a lower bound only when something on the page rests on one', async () => {
    const without = await mountView(
      DashboardView,
      answer(score({ breakdown: [area('DnsSecurity', [{ code: 'DnssecValidationEnabled', values: {} }])] })),
    )
    expect(without.text()).not.toContain('What a lower bound is')

    const withLowerBound = await mountView(
      DashboardView,
      answer(score({ breakdown: [area('PrivacyProtection', [{ code: 'TrackingExposureNone', values: {} }])] })),
    )
    expect(withLowerBound.text()).toContain('What a lower bound is')
  })

  it('offers the explanation of "not measurable" only when an area was not measured', async () => {
    const allMeasured = await mountView(DashboardView, answer(score()))
    expect(allMeasured.text()).not.toContain('What “not measurable” means')

    const oneMissing = await mountView(
      DashboardView,
      answer(score({ breakdown: [area('DeviceHealth', [{ code: 'EnginesNotImplemented', values: {} }], 'NotMeasurable')] })),
    )
    expect(oneMissing.text()).toContain('What “not measurable” means')
  })
})
