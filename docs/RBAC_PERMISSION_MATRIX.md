# Role-based access-control matrix

This document is the Phase 1 authorization contract. API authorization is authoritative; frontend guards only improve navigation and user experience.

## Roles and capabilities

| Capability | Visitor | Learner | Instructor | Content Manager | Manager | Admin |
|---|---:|---:|---:|---:|---:|---:|
| View published public catalog and website content | Yes | Yes | Yes | Yes | Yes | Yes |
| Manage own profile and notifications | — | Own | Own | Own | Own | Own |
| Apply, pay, and view enrollments | — | Own | — | — | Oversight | Full |
| View learner schedule, attendance, materials, assignments | — | Own | — | — | Oversight | Full |
| Manage sessions, attendance, assignments, materials, grades | — | — | Assigned offerings only | — | All | All |
| Edit categories, course/program content, and publication status | — | — | — | Yes | Yes | Yes |
| Manage career paths and their course/program recommendations | — | — | — | Yes | Yes | Yes |
| Edit homepage, FAQ, partners, testimonials, instructor profiles | — | — | — | Yes | — | Yes |
| Set prices, capacity, offerings, schedules, and locations | — | — | — | — | Yes | Yes |
| Review applications and view payment operations | — | Own status | — | — | Yes | Yes |
| Perform administrative payment corrections | — | — | — | — | — | Yes |
| Manage users, account status, and role assignments | — | — | — | — | — | Yes |

## API policy boundaries

- `CatalogEditor` permits `ContentManager`, `Manager`, and `Admin` roles.
- Catalog-editor requests intentionally exclude price, capacity, offering, location, and schedule fields.
- Full course and program mutation endpoints remain protected by `RequireManagerOrAdmin`.
- Course/program offering, scheduling, and training-location endpoints remain protected by `RequireManagerOrAdmin`.
- CMS website-content and notification-template endpoints remain protected by `RequireContentManagerOrAdmin`.
- User and role administration remains protected by `RequireAdmin`.
- Public course, program, and career-path queries return `Published` records only. Non-public records are available through authenticated `/manage` endpoints.
- Instructor services verify assignment ownership in the application/repository layer before exposing rosters, attendance, assignments, materials, submissions, or progress.

## Catalog mutation endpoints

| Endpoint | Policy | Purpose |
|---|---|---|
| `GET /api/v1/courses/manage` | CatalogEditor | Staff listing including draft/archived courses |
| `POST /api/v1/courses/content` | CatalogEditor | Create course editorial content without price |
| `PUT /api/v1/courses/{id}/content` | CatalogEditor | Edit content while preserving price |
| `POST/PUT /api/v1/courses` | Manager/Admin | Full mutation including price |
| `GET /api/v1/programs/manage` | CatalogEditor | Staff listing including draft/archived programs |
| `POST /api/v1/programs/content` | CatalogEditor | Create program editorial content without price |
| `PUT /api/v1/programs/{id}/content` | CatalogEditor | Edit content while preserving price |
| `POST/PUT /api/v1/programs` | Manager/Admin | Full mutation including price |
| `GET /api/v1/career-paths/manage` | CatalogEditor | Staff listing including non-public paths |
| `GET /api/v1/career-paths/manage/{id}` | CatalogEditor | Career-path management detail |
| `POST/PUT/DELETE /api/v1/career-paths` | CatalogEditor | Career-path lifecycle management |

## Frontend route boundaries

- `/cms/catalog`: Content Manager, Manager, Admin.
- `/cms/content` and `/cms/notifications`: Content Manager, Admin.
- `/admin/*` operational routes: Manager, Admin.
- `/admin/users`: Admin only.
- `/admin/payments`: Manager may review; Admin additionally receives corrective actions.
- `/instructor/*`: Instructor role; API ownership checks still apply.
- learner dashboard application, payment, enrollment, learning, attendance, and schedule routes: Learner role.

Phase 1 policies map roles to named policies. A later permission-table implementation can replace the role mapping without changing controller policy names.
