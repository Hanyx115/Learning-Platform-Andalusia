import { render, screen, within } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import * as skillsApi from '../../api/skills'
import SkillsPassportPage from './SkillsPassportPage'

describe('SkillsPassportPage', () => {
  beforeEach(() => {
    vi.restoreAllMocks()
    vi.spyOn(skillsApi, 'getMySkillsPassport').mockResolvedValue({
      assessedSkills: 1,
      demonstratedSkills: 1,
      developingSkills: 0,
      skills: [{
        skillId: 'skill-1',
        name: 'Threat awareness',
        category: 'Cybersecurity',
        description: 'Recognize common digital threats.',
        targetLevel: 2,
        masteryPercentage: 82,
        status: 'Demonstrated',
        gradedEvidenceCount: 2,
        evidence: [{
          learningTitle: 'Cybersecurity Foundations',
          offeringType: 'Course',
          offeringId: 'offering-1',
          gradedAssignments: 2,
          averageScorePercentage: 82,
          lastGradedAtUtc: '2026-11-09T10:00:00Z',
        }],
      }],
    })
  })

  it('shows verified mastery and the graded evidence behind it', async () => {
    render(<SkillsPassportPage />)

    const skill = await screen.findByRole('heading', { name: 'Threat awareness' })
    const card = skill.closest('article')!
    expect(within(card).getByText('82%')).toBeInTheDocument()
    expect(within(card).getByText('Demonstrated')).toBeInTheDocument()
    expect(within(card).getByText('2 graded evidence item(s)')).toBeInTheDocument()
    expect(within(card).getByText('View verified evidence')).toBeInTheDocument()
  })
})
