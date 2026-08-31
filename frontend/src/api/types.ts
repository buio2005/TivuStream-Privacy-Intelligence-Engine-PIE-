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
