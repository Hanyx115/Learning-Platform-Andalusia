export const enTranslations = {
  app: {
    name: 'Andalusia Academy',
  },
  language: {
    label: 'Language',
    arabic: 'العربية',
    english: 'English',
  },
  nav: {
    label: 'Primary navigation',
    open: 'Open menu',
    close: 'Close menu',
    home: 'Home',
    about: 'About',
    courses: 'Courses',
    programs: 'Programs',
    careerPaths: 'Career Paths',
    forBusiness: 'For Business',
    faq: 'FAQ',
    contact: 'Contact',
  },
  footer: {
    navLabel: 'Footer navigation',
    tagline: 'A bilingual academy platform for exploring, applying, and learning.',
    emailLabel: 'hello@andalusia.academy',
    copyright: '© {{year}} Andalusia Academy. All rights reserved.',
  },
  notFound: {
    title: 'Page not found',
    description: 'The requested route does not exist.',
    returnHome: 'Return to the home page',
  },
  about: {
    title: 'About Andalusia Academy',
    description: 'Our story, mission, and approach to bilingual academy education are coming soon.',
  },
  courses: {
    title: 'Courses',
    description: 'The course catalog will be available here once discovery is connected to the API.',
  },
  programs: {
    title: 'Programs',
    description: 'Structured programs made up of multiple courses will be listed here soon.',
  },
  careerPaths: {
    title: 'Career Paths',
    description: 'Recommended learning journeys toward specific careers will be listed here soon.',
  },
  forBusiness: {
    title: 'For Business',
    description: 'Information about corporate training offerings will be available here soon.',
  },
  faq: {
    title: 'Frequently Asked Questions',
    description: 'Answers to common questions about applying, paying, and learning will be listed here soon.',
  },
  contact: {
    title: 'Contact Us',
    description: 'Ways to reach the Andalusia Academy team will be available here soon.',
  },
  home: {
    hero: {
      eyebrow: 'Bilingual academy platform',
      title: 'Explore, apply, and learn, all in one place',
      description:
        'Andalusia Academy connects you to courses, programs, and career paths designed for real progress, in Arabic and English.',
      primaryCta: 'Explore Courses',
      secondaryCta: 'Explore Career Paths',
    },
    careerPaths: {
      eyebrow: 'Career Path Discovery',
      title: 'Find the path that fits your goals',
      description: 'Recommended learning journeys toward specific careers, combining courses and programs.',
      viewAll: 'View all career paths',
      items: [
        {
          title: 'Software Development',
          description: 'Build practical programming skills from the foundations to real projects.',
        },
        {
          title: 'Digital Marketing',
          description: 'Learn to plan, run, and measure modern marketing campaigns.',
        },
        {
          title: 'Business Administration',
          description: 'Develop the management and analytical skills growing businesses need.',
        },
      ],
    },
    categories: {
      eyebrow: 'Popular Categories',
      title: 'Browse by category',
      description: 'Start exploring the subjects learners are choosing most.',
      items: [
        { name: 'Technology', count: '12 courses' },
        { name: 'Business', count: '9 courses' },
        { name: 'Languages', count: '7 courses' },
        { name: 'Design', count: '5 courses' },
      ],
    },
    featured: {
      eyebrow: 'Featured Courses and Programs',
      title: 'Popular with learners right now',
      description: 'A first look at what you can study with Andalusia Academy.',
      viewAll: 'View all courses',
      items: [
        { kind: 'Course', title: 'Web Development Foundations', meta: '8 weeks · Beginner' },
        { kind: 'Program', title: 'Data Analytics Career Track', meta: '4 courses · Intermediate' },
        { kind: 'Course', title: 'Business English Communication', meta: '6 weeks · All levels' },
        { kind: 'Program', title: 'Digital Marketing Professional', meta: '5 courses · Intermediate' },
      ],
    },
    comingSoon: {
      eyebrow: 'Coming Soon',
      title: 'More is on the way',
      description: 'New courses and programs are in development and will launch soon.',
      items: [
        { title: 'Advanced Data Science', meta: 'Coming soon' },
        { title: 'UX Design Fundamentals', meta: 'Coming soon' },
        { title: 'Project Management Professional', meta: 'Coming soon' },
      ],
    },
    forBusiness: {
      eyebrow: 'For Business',
      title: 'Training built for your team',
      description:
        'Andalusia Academy partners with organizations to upskill teams with structured, bilingual learning.',
      highlights: [
        'Bilingual course delivery for diverse teams',
        'Structured learning paths aligned with business goals',
        'Dedicated support for corporate partners',
      ],
      cta: 'Learn About For Business',
    },
    partners: {
      eyebrow: 'Partners and Trust',
      title: 'Building trusted academic and industry relationships',
      description: 'We are building partnerships with organizations that share our commitment to quality education.',
      items: ['Partner One', 'Partner Two', 'Partner Three', 'Partner Four'],
    },
    testimonials: {
      eyebrow: 'Testimonials',
      title: 'What learners are saying',
      description: 'Feedback shared by learners as they progress through their courses.',
      items: [
        {
          quote: 'The bilingual format made it easy to follow along and finally finish a course I could apply at work.',
          name: 'Sara',
          role: 'Business English Communication',
        },
        {
          quote: 'Clear structure and a realistic pace. I could see my progress every week.',
          name: 'Ahmed',
          role: 'Web Development Foundations',
        },
        {
          quote: 'The career path made it obvious which courses to take next instead of guessing.',
          name: 'Layla',
          role: 'Digital Marketing Career Path',
        },
      ],
    },
    cta: {
      title: 'Ready to start your learning journey?',
      description: 'Create an account and explore courses, programs, and career paths today.',
      primaryCta: 'Get Started',
      secondaryCta: 'Learn More',
    },
  },
} as const
