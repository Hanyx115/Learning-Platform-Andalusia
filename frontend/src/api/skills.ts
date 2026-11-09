import { apiRequest } from './client'

export interface SkillEvidence {
  learningTitle: string
  offeringType: 'Course' | 'Program'
  offeringId: string
  gradedAssignments: number
  averageScorePercentage: number
  lastGradedAtUtc: string | null
}

export interface PassportSkill {
  skillId: string
  name: string
  category: string
  description: string
  targetLevel: number
  masteryPercentage: number | null
  status: 'NotAssessed' | 'Developing' | 'Demonstrated'
  gradedEvidenceCount: number
  evidence: SkillEvidence[]
}

export interface SkillsPassport {
  assessedSkills: number
  developingSkills: number
  demonstratedSkills: number
  skills: PassportSkill[]
}

export function getMySkillsPassport() {
  return apiRequest<SkillsPassport>('/api/v1/skills/passport/me')
}
