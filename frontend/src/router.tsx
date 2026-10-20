import { createBrowserRouter, Navigate } from 'react-router-dom'
import RootLayout from './layouts/RootLayout'
import ProtectedRoute from './components/ProtectedRoute'
import AboutPage from './pages/AboutPage'
import ContactPage from './pages/ContactPage'
import ForbiddenPage from './pages/ForbiddenPage'
import ForBusinessPage from './pages/ForBusinessPage'
import HomePage from './pages/HomePage'
import NotFoundPage from './pages/NotFoundPage'
import PlaceholderPage from './pages/PlaceholderPage'
import ForgotPasswordPage from './pages/auth/ForgotPasswordPage'
import LoginPage from './pages/auth/LoginPage'
import ProfilePage from './pages/auth/ProfilePage'
import RegisterPage from './pages/auth/RegisterPage'
import ResetPasswordPage from './pages/auth/ResetPasswordPage'
import CareerPathDetailPage from './pages/catalog/CareerPathDetailPage'
import CareerPathsPage from './pages/catalog/CareerPathsPage'
import CourseDetailPage from './pages/catalog/CourseDetailPage'
import CoursesPage from './pages/catalog/CoursesPage'
import ProgramDetailPage from './pages/catalog/ProgramDetailPage'
import ProgramsPage from './pages/catalog/ProgramsPage'
import ProfileLayout from './pages/profile/ProfileLayout'
import ApplicationsPage from './pages/profile/ApplicationsPage'
import EnrollmentsPage from './pages/profile/EnrollmentsPage'
import StaffApplicationsPage from './pages/staff/StaffApplicationsPage'

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
        path: 'login',
        element: <LoginPage />,
      },
      {
        path: 'register',
        element: <RegisterPage />,
      },
      {
        path: 'forgot-password',
        element: <ForgotPasswordPage />,
      },
      {
        path: 'reset-password',
        element: <ResetPasswordPage />,
      },
      {
        path: 'forbidden',
        element: <ForbiddenPage />,
      },
      {
        element: <ProtectedRoute />,
        children: [
          {
            path: 'applications',
            element: <Navigate to="/profile/applications" replace />,
          },
          {
            path: 'profile',
            element: <ProfileLayout />,
            children: [
              {
                index: true,
                element: <ProfilePage />,
              },
              {
                path: 'applications',
                element: <ApplicationsPage />,
              },
              {
                path: 'enrollments',
                element: <EnrollmentsPage />,
              },
            ],
          },
          {
            path: 'staff/applications',
            element: <StaffApplicationsPage />,
          },
        ],
      },
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
])
