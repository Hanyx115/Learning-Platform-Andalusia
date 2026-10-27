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
import SchedulePage from './pages/profile/SchedulePage'
import AttendancePage from './pages/profile/AttendancePage'
import CourseLearningPage from './pages/profile/CourseLearningPage'
import StaffApplicationsPage from './pages/staff/StaffApplicationsPage'
import InstructorLayout from './pages/instructor/InstructorLayout'
import InstructorSessionsPage from './pages/instructor/InstructorSessionsPage'
import InstructorSessionAttendancePage from './pages/instructor/InstructorSessionAttendancePage'
import InstructorOfferingsPage from './pages/instructor/InstructorOfferingsPage'
import InstructorOfferingAssignmentsPage from './pages/instructor/InstructorOfferingAssignmentsPage'
import InstructorAssignmentSubmissionsPage from './pages/instructor/InstructorAssignmentSubmissionsPage'
import InstructorOfferingProgressPage from './pages/instructor/InstructorOfferingProgressPage'
import InstructorStudentsPage from './pages/instructor/InstructorStudentsPage'
import AdminLayout from './pages/admin/AdminLayout'
import AdminCatalogPage from './pages/admin/AdminCatalogPage'
import AdminLocationsPage from './pages/admin/AdminLocationsPage'
import AdminOfferingsPage from './pages/admin/AdminOfferingsPage'
import AdminSessionsPage from './pages/admin/AdminSessionsPage'
import AdminStudentsPage from './pages/admin/AdminStudentsPage'
import AdminInstructorsPage from './pages/admin/AdminInstructorsPage'
import AdminPaymentsPage from './pages/admin/AdminPaymentsPage'
import AdminUsersPage from './pages/admin/AdminUsersPage'
import AdminCourseMaterialsPage from './pages/admin/AdminCourseMaterialsPage'

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
                element: <ProtectedRoute allowedRoles={['Learner']} />,
                children: [
                  {
                    path: 'applications',
                    element: <ApplicationsPage />,
                  },
                  {
                    path: 'enrollments',
                    element: <EnrollmentsPage />,
                  },
                  {
                    path: 'enrollments/:enrollmentId/learn',
                    element: <CourseLearningPage />,
                  },
                  {
                    path: 'schedule',
                    element: <SchedulePage />,
                  },
                  {
                    path: 'attendance',
                    element: <AttendancePage />,
                  },
                ],
              },
            ],
          },
          {
            element: <ProtectedRoute allowedRoles={['Manager', 'Admin']} />,
            children: [
              {
                path: 'staff/applications',
                element: <StaffApplicationsPage />,
              },
            ],
          },
          {
            element: <ProtectedRoute allowedRoles={['Instructor']} />,
            children: [
              {
                path: 'instructor',
                element: <InstructorLayout />,
                children: [
                  {
                    index: true,
                    element: <Navigate to="/instructor/sessions" replace />,
                  },
                  {
                    path: 'sessions',
                    element: <InstructorSessionsPage />,
                  },
                  {
                    path: 'sessions/:sessionId/attendance',
                    element: <InstructorSessionAttendancePage />,
                  },
                  {
                    path: 'offerings',
                    element: <InstructorOfferingsPage />,
                  },
                  {
                    path: 'courses/:courseId/materials',
                    element: <AdminCourseMaterialsPage />,
                  },
                  {
                    path: 'offerings/:offeringId/assignments',
                    element: <InstructorOfferingAssignmentsPage />,
                  },
                  {
                    path: 'offerings/:offeringId/progress',
                    element: <InstructorOfferingProgressPage />,
                  },
                  {
                    path: 'assignments/:assignmentId/submissions',
                    element: <InstructorAssignmentSubmissionsPage />,
                  },
                  {
                    path: 'students',
                    element: <InstructorStudentsPage />,
                  },
                ],
              },
            ],
          },
          {
            element: <ProtectedRoute allowedRoles={['Admin']} />,
            children: [
              {
                path: 'admin',
                element: <AdminLayout />,
                children: [
                  {
                    index: true,
                    element: <Navigate to="/admin/catalog" replace />,
                  },
                  {
                    path: 'catalog',
                    element: <AdminCatalogPage />,
                  },
                  {
                    path: 'locations',
                    element: <AdminLocationsPage />,
                  },
                  {
                    path: 'courses/:courseId/offerings',
                    element: <AdminOfferingsPage />,
                  },
                  {
                    path: 'courses/:courseId/materials',
                    element: <AdminCourseMaterialsPage />,
                  },
                  {
                    path: 'programs/:programId/offerings',
                    element: <AdminOfferingsPage />,
                  },
                  {
                    path: 'course-offerings/:courseOfferingId/sessions',
                    element: <AdminSessionsPage />,
                  },
                  {
                    path: 'program-offerings/:programOfferingId/sessions',
                    element: <AdminSessionsPage />,
                  },
                  {
                    path: 'course-offerings/:offeringId/assignments',
                    element: <InstructorOfferingAssignmentsPage />,
                  },
                  {
                    path: 'program-offerings/:offeringId/assignments',
                    element: <InstructorOfferingAssignmentsPage />,
                  },
                  {
                    path: 'students',
                    element: <AdminStudentsPage />,
                  },
                  {
                    path: 'instructors',
                    element: <AdminInstructorsPage />,
                  },
                  {
                    path: 'payments',
                    element: <AdminPaymentsPage />,
                  },
                  {
                    path: 'users',
                    element: <AdminUsersPage />,
                  },
                ],
              },
            ],
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
