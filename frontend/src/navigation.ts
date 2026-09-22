export interface NavItem {
  path: string
  labelKey: string
}

export const primaryNavItems: NavItem[] = [
  { path: '/', labelKey: 'nav.home' },
  { path: '/about', labelKey: 'nav.about' },
  { path: '/courses', labelKey: 'nav.courses' },
  { path: '/programs', labelKey: 'nav.programs' },
  { path: '/career-paths', labelKey: 'nav.careerPaths' },
  { path: '/for-business', labelKey: 'nav.forBusiness' },
  { path: '/faq', labelKey: 'nav.faq' },
  { path: '/contact', labelKey: 'nav.contact' },
]
