import { createBrowserRouter, Navigate } from 'react-router-dom'
import RootLayout from './layouts/RootLayout'
import ProtectedRoute from './components/ProtectedRoute'
import AboutPage from './pages/AboutPage'
import ContactPage from './pages/ContactPage'
import ForbiddenPage from './pages/ForbiddenPage'
import ForBusinessPage from './pages/ForBusinessPage'
import HomePage from './pages/HomePage'
import NotFoundPage from './pages/NotFoundPage'
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
import StaffApplicantProfilePage from './pages/staff/StaffApplicantProfilePage'
import InstructorLayout from './pages/instructor/InstructorLayout'
import InstructorSessionsPage from './pages/instructor/InstructorSessionsPage'
import InstructorSessionAttendancePage from './pages/instructor/InstructorSessionAttendancePage'
import InstructorOfferingsPage from './pages/instructor/InstructorOfferingsPage'
import InstructorOfferingAssignmentsPage from './pages/instructor/InstructorOfferingAssignmentsPage'
import InstructorAssignmentSubmissionsPage from './pages/instructor/InstructorAssignmentSubmissionsPage'
import InstructorOfferingProgressPage from './pages/instructor/InstructorOfferingProgressPage'
import InstructorStudentsPage from './pages/instructor/InstructorStudentsPage'
import InstructorOverviewPage from './pages/instructor/InstructorOverviewPage'
import AdminLayout from './pages/admin/AdminLayout'
import AdminOverviewPage from './pages/admin/AdminOverviewPage'
import AdminCatalogPage from './pages/admin/AdminCatalogPage'
import AdminLocationsPage from './pages/admin/AdminLocationsPage'
import AdminOfferingsPage from './pages/admin/AdminOfferingsPage'
import AdminSessionsPage from './pages/admin/AdminSessionsPage'
import AdminStudentsPage from './pages/admin/AdminStudentsPage'
import AdminInstructorsPage from './pages/admin/AdminInstructorsPage'
import AdminPaymentsPage from './pages/admin/AdminPaymentsPage'
import AdminUsersPage from './pages/admin/AdminUsersPage'
import AdminCourseMaterialsPage from './pages/admin/AdminCourseMaterialsPage'
import FaqPage from './pages/FaqPage'
import InstructorsPage from './pages/InstructorsPage'
import NotificationsPage from './pages/profile/NotificationsPage'
import PaymentHistoryPage from './pages/profile/PaymentHistoryPage'
import PaymentCheckoutPage from './pages/profile/PaymentCheckoutPage'
import BalancePage from './pages/profile/BalancePage'
import CmsLayout from './pages/cms/CmsLayout'
import CmsContentPage from './pages/cms/CmsContentPage'
import CmsNotificationsPage from './pages/cms/CmsNotificationsPage'
import AdminCompensationPage from './pages/admin/AdminCompensationPage'
import SkillsPassportPage from './pages/profile/SkillsPassportPage'
import CommunityPage from './pages/community/CommunityPage'

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
        element: <FaqPage />,
      },
      {
        path: 'instructors',
        element: <InstructorsPage />,
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
                path: 'notifications',
                element: <NotificationsPage />,
              },
              {
                path: 'balance',
                element: <BalancePage />,
              },
              {
                element: <ProtectedRoute allowedRoles={['Learner']} />,
                children: [
                  {
                    path: 'applications',
                    element: <ApplicationsPage />,
                  },
                  {
                    path: 'applications/:applicationId/payment',
                    element: <PaymentCheckoutPage />,
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
                  {
                    path: 'payments',
                    element: <PaymentHistoryPage />,
                  },
                  {
                    path: 'skills-passport',
                    element: <SkillsPassportPage />,
                  },
                ],
              },
            ],
          },
          {
            path: 'community/:offeringType/:offeringId',
            element: <CommunityPage />,
          },
          {
            element: <ProtectedRoute allowedRoles={['ContentManager', 'Manager', 'Admin']} />,
            children: [
              {
                path: 'cms',
                element: <CmsLayout />,
                children: [
                  {
                    index: true,
                    element: <Navigate to="/cms/catalog" replace />,
                  },
                  {
                    path: 'catalog',
                    element: <AdminCatalogPage />,
                  },
                  {
                    element: <ProtectedRoute allowedRoles={['ContentManager', 'Admin']} />,
                    children: [
                      {
                        path: 'content',
                        element: <CmsContentPage />,
                      },
                      {
                        path: 'notifications',
                        element: <CmsNotificationsPage />,
                      },
                    ],
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
              {
                path: 'staff/applicants/:userId',
                element: <StaffApplicantProfilePage />,
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
                    element: <InstructorOverviewPage />,
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
            element: <ProtectedRoute allowedRoles={['Manager', 'Admin']} />,
            children: [
              {
                path: 'admin',
                element: <AdminLayout />,
                children: [
                  {
                    index: true,
                    element: <AdminOverviewPage />,
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
                    path: 'compensation',
                    element: <AdminCompensationPage />,
                  },
                  {
                    element: <ProtectedRoute allowedRoles={['Admin']} />,
                    children: [
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
        ],
      },
      {
        path: '*',
        element: <NotFoundPage />,
      },
    ],
  },
])
