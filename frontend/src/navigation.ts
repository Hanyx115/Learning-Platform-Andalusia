export interface NavItem {
  path: string
  labelKey: string
  end?: boolean
}

export interface WorkspaceNavGroup {
  labelKey: string
  items: NavItem[]
}

export const primaryNavItems: NavItem[] = [
  { path: '/', labelKey: 'nav.home' },
  { path: '/about', labelKey: 'nav.about' },
  { path: '/courses', labelKey: 'nav.courses' },
  { path: '/programs', labelKey: 'nav.programs' },
  { path: '/career-paths', labelKey: 'nav.careerPaths' },
  { path: '/for-business', labelKey: 'nav.forBusiness' },
  { path: '/instructors', labelKey: 'nav.instructors' },
  { path: '/faq', labelKey: 'nav.faq' },
  { path: '/contact', labelKey: 'nav.contact' },
]

export function getWorkspaceNavGroups(roles: string[]): WorkspaceNavGroup[] {
  const hasRole = (role: string) => roles.includes(role)
  const groups: WorkspaceNavGroup[] = [
    {
      labelKey: 'profileNav.label',
      items: [
        { path: '/profile', labelKey: 'profileNav.profile', end: true },
        { path: '/profile/notifications', labelKey: 'profileNav.notifications' },
        { path: '/profile/balance', labelKey: 'profileNav.balance' },
        ...(hasRole('Learner') ? [
          { path: '/profile/applications', labelKey: 'profileNav.applications' },
          { path: '/profile/enrollments', labelKey: 'profileNav.enrollments' },
          { path: '/profile/schedule', labelKey: 'profileNav.schedule' },
          { path: '/profile/attendance', labelKey: 'profileNav.attendance' },
          { path: '/profile/payments', labelKey: 'profileNav.payments' },
        ] : []),
      ],
    },
  ]

  if (hasRole('Instructor')) {
    groups.push({
      labelKey: 'instructorNav.label',
      items: [
        { path: '/instructor/sessions', labelKey: 'instructorNav.sessions' },
        { path: '/instructor/offerings', labelKey: 'instructorNav.offerings' },
        { path: '/instructor/students', labelKey: 'instructorNav.students' },
      ],
    })
  }

  if (hasRole('ContentManager') || hasRole('Manager') || hasRole('Admin')) {
    groups.push({
      labelKey: 'cmsNav.label',
      items: [
        { path: '/cms/catalog', labelKey: 'cmsNav.catalog' },
        ...((hasRole('ContentManager') || hasRole('Admin')) ? [
          { path: '/cms/content', labelKey: 'cmsNav.content' },
          { path: '/cms/notifications', labelKey: 'cmsNav.notifications' },
        ] : []),
      ],
    })
  }

  if (hasRole('Manager') || hasRole('Admin')) {
    groups.push({
      labelKey: 'adminNav.label',
      items: [
        { path: '/staff/applications', labelKey: 'nav.reviewApplications' },
        { path: '/admin/catalog', labelKey: 'adminNav.catalog' },
        { path: '/admin/locations', labelKey: 'adminNav.locations' },
        { path: '/admin/students', labelKey: 'adminNav.students' },
        { path: '/admin/instructors', labelKey: 'adminNav.instructors' },
        { path: '/admin/payments', labelKey: 'adminNav.payments' },
        { path: '/admin/compensation', labelKey: 'adminNav.compensation' },
        ...(hasRole('Admin') ? [{ path: '/admin/users', labelKey: 'adminNav.users' }] : []),
      ],
    })
  }

  return groups
}
