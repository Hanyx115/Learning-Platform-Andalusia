# Andalusia Academy Phase 1 Conceptual Class Diagrams

## Status and intent

These diagrams describe the intended Clean Architecture boundaries and the main Phase 1 domain relationships. They are not generated code and do not freeze constructors, methods, namespaces, or persistence mappings.

## 1. Solution boundaries

```mermaid
classDiagram
    namespace Domain {
        class DomainModel {
            <<business rules>>
        }
        class DomainEvents {
            <<contracts>>
        }
    }

    namespace Application {
        class Commands {
            <<use cases>>
        }
        class Queries {
            <<use cases>>
        }
        class Validators {
            <<input boundaries>>
        }
        class ApplicationInterfaces {
            <<ports>>
        }
    }

    namespace Infrastructure {
        class PersistenceAdapters {
            <<EF Core and SQL Server>>
        }
        class IdentityAdapters {
            <<ASP.NET Core Identity>>
        }
        class ProviderAdapters {
            <<payments, notifications, storage>>
        }
    }

    namespace API {
        class Controllers {
            <<thin HTTP endpoints>>
        }
        class Middleware {
            <<cross-cutting HTTP behavior>>
        }
        class CompositionRoot {
            <<dependency injection>>
        }
    }

    Commands --> DomainModel
    Queries --> DomainModel
    Commands --> ApplicationInterfaces
    Queries --> ApplicationInterfaces
    Validators --> Commands
    Validators --> Queries
    PersistenceAdapters ..|> ApplicationInterfaces
    IdentityAdapters ..|> ApplicationInterfaces
    ProviderAdapters ..|> ApplicationInterfaces
    Controllers --> Commands
    Controllers --> Queries
    CompositionRoot --> ApplicationInterfaces
    CompositionRoot --> PersistenceAdapters
    CompositionRoot --> IdentityAdapters
    CompositionRoot --> ProviderAdapters
    DomainModel --> DomainEvents
```

Required project dependency direction:

```text
Domain          -> no project dependency
Application     -> Domain
Infrastructure  -> Application + Domain
API             -> Application + Infrastructure
```

## 2. Catalog aggregate model

```mermaid
classDiagram
    class Course {
        +Guid Id
        +Guid CategoryId
        +string Slug
        +CourseStatus Status
        +bool IsDeleted
        +Publish()
        +Archive()
    }
    class CourseTranslation {
        +string LanguageCode
        +string Title
        +string Description
    }
    class CourseOffering {
        +Guid Id
        +DateTimeOffset StartsAt
        +DateTimeOffset EndsAt
        +Money Price
        +int Capacity
        +OfferingStatus Status
    }
    class CourseSession {
        +Guid Id
        +DateTimeOffset StartsAt
        +DateTimeOffset EndsAt
    }
    class Program {
        +Guid Id
        +string Slug
        +ProgramStatus Status
        +bool IsDeleted
        +Publish()
        +Archive()
    }
    class ProgramCourse {
        +Guid ProgramId
        +Guid CourseId
        +int Sequence
        +bool IsRequired
    }
    class ProgramOffering {
        +Guid Id
        +DateTimeOffset StartsAt
        +DateTimeOffset EndsAt
        +Money Price
        +int Capacity
    }
    class CareerPath {
        +Guid Id
        +string Slug
        +PublishingStatus Status
    }
    class TrainingLocation {
        +Guid Id
        +string BranchName
        +string RoomName
        +bool IsActive
    }
    class InstructorProfile {
        +Guid UserId
        +PublishingStatus Status
    }
    class Money {
        <<value object>>
        +decimal Amount
        +string Currency
    }

    Course "1" *-- "1..*" CourseTranslation
    Course "1" *-- "0..*" CourseOffering
    CourseOffering "1" *-- "0..*" CourseSession
    CourseOffering --> TrainingLocation
    CourseSession --> TrainingLocation
    CourseSession --> InstructorProfile
    Program "1" *-- "1..*" ProgramCourse
    ProgramCourse --> Course
    Program "1" *-- "0..*" ProgramOffering
    ProgramOffering --> TrainingLocation
    CareerPath --> Program
    CareerPath --> Course
    CourseOffering --> Money
    ProgramOffering --> Money
```

`Publish()` and `Archive()` represent responsibilities, not approved method signatures. Publishing rules must require the approved bilingual content and scheduling data.

## 3. Application, payment, and enrollment aggregates

```mermaid
classDiagram
    class Application {
        <<aggregate root>>
        +Guid Id
        +Guid ApplicantId
        +Guid? CourseOfferingId
        +Guid? ProgramOfferingId
        +ApplicationStatus Status
        +DateTimeOffset SubmittedAt
        +Submit()
        +ChangeStatus()
    }
    class ApplicationStatusHistory {
        +ApplicationStatus FromStatus
        +ApplicationStatus ToStatus
        +Guid ChangedByUserId
        +string Reason
        +DateTimeOffset ChangedAt
    }
    class Payment {
        <<aggregate root>>
        +Guid Id
        +Guid ApplicationId
        +Money Amount
        +PaymentStatus Status
        +string IdempotencyKey
        +RecordAttempt()
        +ApplyVerifiedStatus()
    }
    class PaymentAttempt {
        +Guid Id
        +string Provider
        +string ProviderReference
        +PaymentStatus Status
    }
    class PaymentStatusHistory {
        +PaymentStatus Status
        +string Source
        +DateTimeOffset ChangedAt
    }
    class Enrollment {
        <<aggregate root>>
        +Guid Id
        +Guid LearnerId
        +Guid ApplicationId
        +Guid? CourseOfferingId
        +Guid? ProgramOfferingId
        +EnrollmentStatus Status
        +DateTimeOffset EnrolledAt
        +Complete()
        +Cancel()
    }
    class PaymentWebhookReceipt {
        +string ProviderEventId
        +string PayloadHash
        +bool SignatureValid
        +ProcessingStatus Status
    }
    class IdempotencyRecord {
        +string Scope
        +string Key
        +string RequestHash
        +string ResponseReference
    }
    class Money {
        <<value object>>
        +decimal Amount
        +string Currency
    }

    Application "1" *-- "0..*" ApplicationStatusHistory
    Application "1" --> "0..1" Payment
    Application "1" --> "0..1" Enrollment
    Payment "1" *-- "0..*" PaymentAttempt
    Payment "1" *-- "0..*" PaymentStatusHistory
    Payment --> Money
    PaymentWebhookReceipt ..> Payment : verified update
    IdempotencyRecord ..> Payment : protects
    Enrollment ..> Payment : created after confirmation
```

Core invariants:

- `Application` and `Enrollment` each reference exactly one offering type.
- Browser input never marks a payment as paid.
- Payment initiation and webhook processing are retry-safe.
- Verified payment, enrollment creation, and the corresponding outbox event share a transaction where possible.
- Enrollment and financial history cannot be physically deleted.

> **⚠ BUSINESS RULE TO CONFIRM:** The allowed application, payment, and enrollment state transitions are not final.

## 4. LMS aggregate model

```mermaid
classDiagram
    class Enrollment {
        <<aggregate root reference>>
        +Guid Id
        +Guid LearnerId
    }
    class CourseSession {
        +Guid Id
        +Guid CourseOfferingId
    }
    class CourseMaterial {
        +Guid Id
        +Guid CourseId
        +Guid? SessionId
        +Guid? MediaAssetId
        +MaterialVisibility Visibility
    }
    class Attendance {
        +Guid Id
        +Guid SessionId
        +Guid EnrollmentId
        +AttendanceStatus Status
        +Guid MarkedByUserId
        +Mark()
        +Correct()
    }
    class Assignment {
        <<aggregate root>>
        +Guid Id
        +Guid CourseOfferingId
        +Guid? SessionId
        +string Title
        +DateTimeOffset DueAt
        +PublishingStatus Status
        +Publish()
    }
    class AssignmentSubmission {
        +Guid Id
        +Guid AssignmentId
        +Guid EnrollmentId
        +DateTimeOffset SubmittedAt
        +SubmissionStatus Status
        +Submit()
    }
    class MaterialProgress {
        +Guid EnrollmentId
        +Guid CourseMaterialId
        +DateTimeOffset? CompletedAt
    }
    class SessionProgress {
        +Guid EnrollmentId
        +Guid SessionId
        +ProgressStatus Status
    }
    class CourseProgress {
        +Guid EnrollmentId
        +decimal Percentage
        +ProgressStatus Status
        +int CalculationVersion
    }

    CourseSession "1" --> "0..*" Attendance
    Enrollment "1" --> "0..*" Attendance
    Assignment "1" *-- "0..*" AssignmentSubmission
    Enrollment "1" --> "0..*" AssignmentSubmission
    Enrollment "1" --> "0..*" MaterialProgress
    CourseMaterial "1" --> "0..*" MaterialProgress
    Enrollment "1" --> "0..*" SessionProgress
    CourseSession "1" --> "0..*" SessionProgress
    Enrollment "1" --> "0..1" CourseProgress
```

> **⚠ BUSINESS RULE TO CONFIRM:** `Correct()`, `Submit()`, and course-progress calculation cannot be finalized until attendance corrections, assignment resubmissions, scoring, and progress weights are approved.

## 5. Notification and reliability model

```mermaid
classDiagram
    class NotificationTemplate {
        <<aggregate root>>
        +Guid Id
        +string Code
        +NotificationChannel[] AllowedChannels
        +bool IsActive
    }
    class NotificationTemplateTranslation {
        +string LanguageCode
        +NotificationChannel Channel
        +string Subject
        +string Body
    }
    class Notification {
        <<aggregate root>>
        +Guid Id
        +Guid RecipientUserId
        +string EventType
        +DateTimeOffset? ReadAt
        +MarkRead()
    }
    class NotificationDelivery {
        +Guid Id
        +NotificationChannel Channel
        +DeliveryStatus Status
        +int AttemptCount
        +RecordAttempt()
    }
    class OutboxMessage {
        +Guid Id
        +string MessageType
        +string PayloadJson
        +OutboxStatus Status
        +DateTimeOffset OccurredAt
        +MarkProcessed()
    }
    class AuditLog {
        +Guid Id
        +Guid? ActorUserId
        +string Action
        +string EntityType
        +string EntityId
        +string CorrelationId
        +DateTimeOffset OccurredAt
    }

    NotificationTemplate "1" *-- "1..*" NotificationTemplateTranslation
    Notification --> NotificationTemplate
    Notification "1" *-- "0..*" NotificationDelivery
    OutboxMessage ..> Notification : triggers
    AuditLog ..> Notification : audits sensitive actions
```

Notification delivery failure must not roll back a completed payment or enrollment transaction. Delivery workers and outbox consumers must be idempotent.

## Implementation gate

Before these concepts become C# classes:

1. Confirm aggregate ownership and transaction boundaries.
2. Confirm state machines and unresolved business rules.
3. Keep provider and EF Core concerns out of Domain.
4. Add only the classes needed by the current vertical slice.
5. Review the corresponding ERD constraints before generating a migration.
