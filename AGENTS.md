# Andalusia Academy Codex Instructions

## Project purpose

Build the Andalusia Academy Phase 1 MVB as an end-to-end bilingual Website and LMS. The required journey is:

Visitor -> Explore -> View details -> Create account -> Apply -> Pay -> Enroll -> Learner dashboard -> Course access -> Progress.

The repository is maintained by Ahmed Hany and one other developer with similar experience. Preserve compatible work from both developers and keep every change easy to review.

## Required reading

Before proposing architecture, changing database design, or implementing a major feature, read the relevant current files:

1. `README.md` - approved Stage 0 architecture and roadmap.
2. `docs/Andalusia_Academy_Master_Project_Prompt.txt` - detailed project rules.
3. `docs/Andalusia_Academy_Phase_1_MVB_Scope.pdf` - authoritative product scope.
4. `docs/Andalusia_Academy_Phase1_Technical_Architecture_v2.pages` - supporting engineering plan.
5. The current source code, migrations, Git branch, and recent commits.

Do not repeatedly load every document for a small isolated change. Read the files relevant to the requested work and inspect the current implementation first.

## Source-of-truth priority

When sources conflict, follow this order:

1. The user's latest explicit instruction.
2. The current codebase, branch, migrations, and API contracts.
3. The Phase 1 MVB Feature Scope.
4. The Technical Architecture and Engineering Plan.
5. Decisions in `README.md` that have not been superseded.
6. Earlier generated code or recommendations.

Never restore old generated code over newer compatible work.

Use this exact label when a required business rule is missing:

`⚠ BUSINESS RULE TO CONFIRM`

Use this exact label before any necessary incompatible change:

`⚠ BREAKING CHANGE`

Separate recommendations from official requirements with:

`RECOMMENDATION`

## Fixed technology stack

- Backend: ASP.NET Core Web API, .NET 10, C#.
- Data: Entity Framework Core, SQL Server, Code First migrations.
- API: REST, Swagger/OpenAPI, versioned `/api/v1/...` routes.
- Frontend: React 18, TypeScript strict mode, Vite.
- Architecture: pragmatic Clean Architecture with controllers, application services, repository abstractions, and Infrastructure implementations in a modular monolith.
- Validation: FluentValidation.
- Authentication: ASP.NET Core Identity, JWT access tokens, rotating refresh tokens.
- Frontend server state: TanStack Query.
- Frontend client-only global state: Zustand only when genuinely needed.

Do not silently replace Vite with Next.js or replace .NET 10 with another target.

Do not introduce microservices, event sourcing, distributed databases, Kubernetes, Redis, Hangfire, Blob Storage, or other infrastructure before the current feature justifies it.

## Solution boundaries

The backend uses these projects:

- `AndalusiaAcademy.Domain`
- `AndalusiaAcademy.Application`
- `AndalusiaAcademy.Infrastructure`
- `AndalusiaAcademy.API`

Dependency direction:

- Domain has no dependency on Application, Infrastructure, or API.
- Application depends on Domain.
- Infrastructure depends on Application and Domain.
- API depends on Application and Infrastructure and acts as the composition root.

Logical modules:

- Catalog
- Identity
- Enrollment
- LMS Core
- Operations
- CMS
- Notifications

Keep one deployable application and one SQL Server database in Phase 1.

Use these readable folder conventions as features are added:

- Domain models: `AndalusiaAcademy.Domain/Models`.
- DTOs and business services: `AndalusiaAcademy.Application/DTOs` and `AndalusiaAcademy.Application/Services`.
- Repository abstractions: `AndalusiaAcademy.Application/Repositories`.
- EF Core data access and migrations: `AndalusiaAcademy.Infrastructure/Data`.
- Repository implementations: `AndalusiaAcademy.Infrastructure/Repositories`.
- HTTP endpoints: `AndalusiaAcademy.API/Controllers`.

Do not create empty placeholder folders. Add each folder when its first real type is implemented.

## Normal request flow

Use this normal flow:

React -> Controller -> Application service -> Repository abstraction -> Infrastructure repository -> EF Core -> SQL Server.

- Keep controllers thin.
- Put use-case and business orchestration logic in focused application services.
- Put domain invariants in Domain.
- Put repository interfaces in Application and their EF Core implementations in Infrastructure.
- Put provider and persistence details in Infrastructure.
- Do not add MediatR, command/query handlers, managers, or another application pipeline unless a concrete feature requires it and the user approves it.
- Use ASP.NET Core Identity's managers and stores directly instead of wrapping them in redundant repositories.
- Never expose EF Core entities directly from API endpoints.

## Scope controls

Phase 1 includes public discovery, identity, application, payment, enrollment, basic LMS, attendance, assignments, progress, role dashboards, CMS, permissions, and notifications.

Do not implement these without explicit approval:

- Direct instructor-learner messaging.
- Assessments, quizzes, question banks, or advanced grading.
- Certificates.
- AI assessment, AI assistant, or AI recommendations.
- Gamification.
- Advanced analytics or at-risk engines.
- Full Corporate Academy accounts, employees, bulk enrollment, or corporate reporting.
- Alumni, community, or advanced marketing automation.

The public For Business page is informational in Phase 1.

## Change discipline

Before changing code:

1. Inspect `git status`, the active branch, current files, and affected tests.
2. Identify the smallest set of affected files and dependencies.
3. Preserve existing compatible behavior and colleague changes.
4. State any database, API, security, or frontend contract impact.
5. Apply the smallest safe change.
6. Build and test the affected area.
7. Verify database effects when applicable.
8. Provide the Git checkpoint and colleague handoff.

Do not rewrite entire files, classes, modules, or the codebase for a small change. Do not perform unrelated refactors.

If a larger refactor is necessary, explain why a minimal patch is insufficient before performing it.

## Code quality

Generate complete, compile-ready implementation code. Do not leave placeholder methods or ask Ahmed to finish missing implementation.

Use comments only for non-obvious rules, security decisions, transactions, idempotency, performance-sensitive queries, and intentional tradeoffs. Prefer self-documenting names.

Use:

- Async database and external I/O.
- `CancellationToken` where appropriate.
- Request and response DTOs.
- Consistent ProblemDetails-style errors.
- Global exception handling rather than controller try/catch duplication.
- Structured logging without secrets or sensitive payloads.
- Appropriate unit and integration tests for important behavior.

## Database and EF Core rules

- Filter, search, sort, group, project, and paginate in SQL whenever possible.
- Prefer `Where`, `OrderBy`, `Select`, `Skip`, and `Take` before materialization.
- Use `AsNoTracking` for read-only entity queries where appropriate.
- Prefer DTO projections for read APIs.
- Avoid N+1 queries and repeated database round trips.
- Do not use `Include` or manual `Join` blindly.
- Explain significant joins/projections and useful indexes.
- Enforce important invariants with PKs, FKs, unique constraints, required columns, check constraints, transactions, and appropriate concurrency protection.
- Preserve historical payment, enrollment, progress, and attendance data.
- Use soft delete for Courses and Programs when historical data depends on them.
- Never casually hard-delete enrollments or financial history.

Before creating a migration:

1. Confirm the branch is synchronized safely.
2. Inspect existing migrations and the model snapshot.
3. Check whether the colleague created a migration.
4. Resolve model conflicts first.
5. Generate the migration only after the model is coherent.

Never delete or rewrite a shared/deployed migration casually.

## Security and integrity

- Backend authorization is authoritative; hiding frontend controls is not authorization.
- Use granular permission policies on sensitive endpoints.
- Verify ownership for learner- and instructor-scoped resources.
- Never store plaintext passwords, raw refresh tokens, card data, or secrets.
- Use User Secrets locally and secure secret management for deployment.
- Never trust the browser for final payment status.
- Validate payment webhook signatures and make processing retry-safe.
- Protect payment and enrollment writes with idempotency, unique constraints, transactions, and audit/correlation information.
- Do not sacrifice authorization, validation, integrity, or auditability for performance.
- Treat PDPL implementation details and Azure data residency as stakeholder/legal decisions.

## Localization and accessibility

- Arabic-first and RTL-native with full English support.
- Do not hard-code user-facing strings throughout React components.
- Support root `dir="rtl"` and `dir="ltr"` switching.
- Prefer CSS logical properties.
- Verify important components in both directions.
- Target WCAG 2.1 AA for important flows.
- Do not duplicate every database column into Arabic/English fields without following the approved translation strategy.

## Testing and verification

Do not declare implementation complete because code was written.

For backend work, verify as applicable:

- `dotnet restore`
- `dotnet build`
- Relevant unit/integration tests
- Swagger endpoint behavior
- Authentication/authorization requirements
- Positive and negative cases
- SQL Server schema and data effects

For frontend work, verify as applicable:

- Dependency installation
- TypeScript checking
- Linting
- Production build
- RTL/LTR behavior
- API contract compatibility

If execution is blocked, state exactly what was verified and what remains for local execution.

## Git workflow

Branches:

- `main` - stable/release-ready.
- `develop` - shared integration.
- Task branches from `develop`: `feature/*`, `fix/*`, `refactor/*`, `chore/*`, `docs/*`, `test/*`.

Normal flow:

1. Check `git status`.
2. Synchronize `develop` safely.
3. Create one focused task branch.
4. Commit coherent, working changes.
5. Push the task branch.
6. Open a Pull Request into `develop`.
7. Promote `develop` to `main` only through a release PR.

Use Conventional Commits, for example:

- `chore(solution): initialize clean architecture projects`
- `refactor(backend): adopt readable layered structure`
- `feat(auth): add JWT authentication`
- `feat(courses): implement paginated course catalog`
- `fix(payments): prevent duplicate webhook processing`
- `test(auth): add login integration tests`

Never force-push `main` or `develop`, reset shared work destructively, or commit secrets.

At the end of a meaningful implementation step, report:

- Current branch.
- Whether to commit now.
- Exact files to stage.
- Exact commit message.
- Whether to push.
- Whether to open a PR and its target/title.
- Safe synchronization instructions for the next step.

Also provide a concise colleague handoff covering implementation, important files, database changes, APIs, permissions, tests, open business rules, branch, commit, and PR status.

## Current project state

- Stage 0 requirements and architecture analysis is complete in `README.md`.
- The solution, API foundation, frontend foundation, Docker development support, EF Core context, and initial database migration exist.
- The current backend architecture uses readable project and folder boundaries with Controller -> Service -> Repository -> EF Core flow.
- The database-foundation branch is intentionally awaiting colleague review and must not be merged into `develop` without the user's instruction.
- The next business checkpoint after the architecture refactor is Identity foundation.

## Session-start response

When updated code or colleague work is provided, begin with:

`CURRENT STATUS`

- `COMPLETED:`
- `COLLEAGUE CHANGES:`
- `CURRENT BRANCH / EXPECTED BRANCH:`
- `POTENTIAL CONFLICTS:`
- `NEXT SAFE TASK:`

Then inspect the actual repository and proceed incrementally. Stop at meaningful checkpoints.
