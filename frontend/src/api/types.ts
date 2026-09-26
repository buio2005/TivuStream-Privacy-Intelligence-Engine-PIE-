// Shapes returned by the Privacy Intelligence Engine REST API.
//
// They mirror the Unified Data Model. Nothing here carries text meant to be
// shown: the API returns facts, and the interface renders them in words.

export interface ApiResponse<T> {
  success: boolean
  apiVersion: string
  timestamp: string
  data: T | null
  error: ApiError | null
}

export interface ApiError {
  code: string
  message: string

  /** Precise cause, for the interface to say in the reader's language. */
  reason?: string
}

/** A reason that determined the score of an area. */
export interface ScoreFactor {
  code: string
  values: Record<string, unknown>
}

export type ScoreComponentState = 'Measured' | 'PartiallyMeasured' | 'NotMeasurable'

export interface ScoreComponent {
  component: string
  state: ScoreComponentState
  score: number
  maxScore: number
  weight: number
  factors: ScoreFactor[]
}

export interface Npss {
  /** Absent when coverage is below the minimum. Never replaced by a stand-in. */
  overallScore: number | null
  status: string | null
  trend: string | null
  coverage: number
  algorithmVersion: string
  generatedAt: string
  breakdown: ScoreComponent[]
}

export type MeasurementQuality = 'Exact' | 'LowerBound' | 'PeriodBounded' | 'Estimated'

export type ConfidenceLevel = 'Low' | 'Medium' | 'High'

export interface Domain {
  name: string
  category: string

  /** Absent when the category is Unknown: there is no list to attribute it to. */
  categoryConfidence: ConfidenceLevel | null
  categorySource: string | null
  categorySourceUpdatedAt: string | null

  reputation: string | null
  firstSeen: string
  lastSeen: string
  observationQuality: MeasurementQuality
  occurrences: number
}

export interface ObservationPeriod {
  start: string
  end: string
}

export interface ObservedDomains {
  /** Absent when nothing was ever observed. */
  period: ObservationPeriod | null

  /** Hourly periods that exist, told apart from those asked for. */
  periodsObserved: number
  periodsRequested: number

  domains: Domain[]
}

/** What the list of activities of a domain means. */
export type ActivityAccess = 'Available' | 'Unavailable' | 'Withheld'

export type DeviceIdentityBasis = 'NetworkAddress' | 'HardwareAddress'

/**
 * Who a device is, as far as the interval tells.
 *
 * The address and the basis are absent together when no period of the
 * interval describes the device: only its identifier is known.
 */
export interface DeviceIdentification {
  deviceId: string
  hostname: string | null
  ipAddress: string | null
  identityBasis: DeviceIdentityBasis | null
}

/** Activity of one device towards a domain, over the interval. */
export interface ObservedActivity {
  device: DeviceIdentification
  queryCount: number
  blocked: boolean

  /** As the source names it. The documentation does not close the set. */
  protocol: string

  firstSeen: string
  lastSeen: string
  observationQuality: MeasurementQuality
}

/** A domain over the same interval as the list, with its activity. */
export interface DomainDetail {
  period: ObservationPeriod | null
  periodsObserved: number
  periodsRequested: number
  domain: Domain

  /** Meaningful only when `activityAccess` is `Available`. */
  activities: ObservedActivity[]
  activityAccess: ActivityAccess
}

export type AccountRole = 'Administrator' | 'Viewer'

/** Who is signed in, as `auth/session` describes it. */
export interface SessionInfo {
  username: string
  role: AccountRole
  passwordChangeRequired: boolean
  expiresAt: string
}

/** An account, as an administrator sees it. The password never travels back. */
export interface AccountSummary {
  username: string
  role: AccountRole
  enabled: boolean
  passwordChangeRequired: boolean
  createdAt: string
}
