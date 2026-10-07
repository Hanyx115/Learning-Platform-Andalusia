import { createBrowserRouter } from 'react-router-dom'
import RootLayout from './layouts/RootLayout'
import AboutPage from './pages/AboutPage'
import ContactPage from './pages/ContactPage'
import ForBusinessPage from './pages/ForBusinessPage'
import HomePage from './pages/HomePage'
import NotFoundPage from './pages/NotFoundPage'
import PlaceholderPage from './pages/PlaceholderPage'
import CareerPathDetailPage from './pages/catalog/CareerPathDetailPage'
import CareerPathsPage from './pages/catalog/CareerPathsPage'
import CourseDetailPage from './pages/catalog/CourseDetailPage'
import CoursesPage from './pages/catalog/CoursesPage'
import ProgramDetailPage from './pages/catalog/ProgramDetailPage'
import ProgramsPage from './pages/catalog/ProgramsPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <RootLayout />,
    children: [
      {
        index: true,
        element: <HomePage />,
      },
      {
        path: 'about',
        element: <AboutPage />,
      },
      {
        path: 'courses',
        element: <CoursesPage />,
      },
      {
        path: 'courses/:slug',
        element: <CourseDetailPage />,
      },
      {
        path: 'programs',
        element: <ProgramsPage />,
      },
      {
        path: 'programs/:slug',
        element: <ProgramDetailPage />,
      },
      {
        path: 'career-paths',
        element: <CareerPathsPage />,
      },
      {
        path: 'career-paths/:slug',
        element: <CareerPathDetailPage />,
      },
      {
        path: 'for-business',
        element: <ForBusinessPage />,
      },
      {
        path: 'faq',
        element: <PlaceholderPage titleKey="faq.title" descriptionKey="faq.description" />,
      },
      {
        path: 'contact',
        element: <ContactPage />,
      },
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
])
