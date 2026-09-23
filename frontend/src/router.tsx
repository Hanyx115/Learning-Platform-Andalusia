import { createBrowserRouter } from 'react-router-dom'
import RootLayout from './layouts/RootLayout'
import AboutPage from './pages/AboutPage'
import ContactPage from './pages/ContactPage'
import HomePage from './pages/HomePage'
import NotFoundPage from './pages/NotFoundPage'
import PlaceholderPage from './pages/PlaceholderPage'

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
        element: <PlaceholderPage titleKey="courses.title" descriptionKey="courses.description" />,
      },
      {
        path: 'programs',
        element: <PlaceholderPage titleKey="programs.title" descriptionKey="programs.description" />,
      },
      {
        path: 'career-paths',
        element: <PlaceholderPage titleKey="careerPaths.title" descriptionKey="careerPaths.description" />,
      },
      {
        path: 'for-business',
        element: <PlaceholderPage titleKey="forBusiness.title" descriptionKey="forBusiness.description" />,
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
