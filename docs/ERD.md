# Andalusia Academy Phase 1 Conceptual ERD

## Status and scope

This is the proposed conceptual entity-relationship model for the Phase 1 MVB. It is a design checkpoint, not an implemented database schema or migration contract.

The model covers the approved Phase 1 journey:

```text
Visitor -> Explore -> Apply -> Pay -> Enroll -> Learn -> Progress
```

It intentionally excludes Phase 2 capabilities such as direct messaging, assessments, quizzes, certificates, gamification, advanced analytics, and AI features.

## Data conventions

- Business identifiers use index-friendly `Guid` values.
- Business and audit timestamps use UTC `DateTimeOffset`.
- Money uses `decimal(18,2)` plus a three-character currency code.
- Arabic and English searchable content uses translation tables.
- Uploaded binaries remain outside SQL Server; `MediaAsset` stores metadata and a storage key.
- Payment, enrollment, attendance, progress, payout, and audit history is preserved.
- Important invariants must be enforced in SQL Server with keys, indexes, check constraints, and transactions.

## 1. Identity and authorization

```mermaid
erDiagram
    APPLICATION_USER {
        guid id PK
        string normalized_email UK
        string first_name
        string last_name
        string phone
        string country_code
        string preferred_language
        datetimeoffset consented_at
        bool is_active
    }
    LEARNER_PROFILE {
        guid user_id PK,FK
    }
    INSTRUCTOR_PROFILE {
        guid user_id PK,FK
        guid media_asset_id FK
        string publishing_status
        bool is_deleted
    }
    INSTRUCTOR_PROFILE_TRANSLATION {
        guid instructor_profile_id PK,FK
        string language_code PK
        string headline
        string biography
        string expertise
    }
    APPLICATION_ROLE {
        guid id PK
        string normalized_name UK
    }
    APPLICATION_USER_ROLE {
        guid user_id PK,FK
        guid role_id PK,FK
    }
    PERMISSION {
        guid id PK
        string code UK
        string description
    }
    ROLE_PERMISSION {
        guid role_id PK,FK
        guid permission_id PK,FK
    }
    REFRESH_TOKEN {
        guid id PK
        guid user_id FK
        string token_hash UK
        string family_id
        datetimeoffset expires_at
        datetimeoffset revoked_at
        guid replaced_by_token_id FK
    }
    MEDIA_ASSET {
        guid id PK
        string storage_key UK
    }

    APPLICATION_USER ||--o| LEARNER_PROFILE : has
    APPLICATION_USER ||--o| INSTRUCTOR_PROFILE : has
    INSTRUCTOR_PROFILE ||--o{ INSTRUCTOR_PROFILE_TRANSLATION : localizes
    MEDIA_ASSET o|--o| INSTRUCTOR_PROFILE : illustrates
    APPLICATION_USER ||--o{ APPLICATION_USER_ROLE : receives
    APPLICATION_ROLE ||--o{ APPLICATION_USER_ROLE : assigns
    APPLICATION_ROLE ||--o{ ROLE_PERMISSION : grants
    PERMISSION ||--o{ ROLE_PERMISSION : belongs_to
    APPLICATION_USER ||--o{ REFRESH_TOKEN : owns
    REFRESH_TOKEN o|--o| REFRESH_TOKEN : replaces
```

ASP.NET Core Identity will also supply its standard claim, login, and token tables. Raw refresh tokens must never be stored.

## 2. Catalog and scheduling

```mermaid
erDiagram
    COURSE_CATEGORY {
        guid id PK
        string slug UK
        bool is_active
    }
    COURSE {
        guid id PK
        guid category_id FK
        guid media_asset_id FK
        string slug UK
        string course_type
        string status
        int duration_minutes
        datetimeoffset published_at
        bool is_deleted
    }
    COURSE_TRANSLATION {
        guid course_id PK,FK
        string language_code PK
        string title
        string short_description
        string full_description
        string objectives_json
        string seo_title
        string seo_description
    }
    COURSE_INSTRUCTOR {
        guid course_id PK,FK
        guid instructor_id PK,FK
        bool is_lead
    }
    TRAINING_LOCATION {
        guid id PK
        string branch_name
        string room_name
        string city
        string address
        bool is_active
    }
    COURSE_OFFERING {
        guid id PK
        guid course_id FK
        guid location_id FK
        datetimeoffset starts_at
        datetimeoffset ends_at
        decimal price
        string currency
        int capacity
        string status
    }
    COURSE_SESSION {
        guid id PK
        guid course_offering_id FK
        guid instructor_id FK
        guid location_id FK
        datetimeoffset starts_at
        datetimeoffset ends_at
    }
    PROGRAM_CATEGORY {
        guid id PK
        string slug UK
        bool is_active
    }
    PROGRAM {
        guid id PK
        guid category_id FK
        guid media_asset_id FK
        string slug UK
        string status
        bool is_deleted
    }
    PROGRAM_TRANSLATION {
        guid program_id PK,FK
        string language_code PK
        string title
        string overview
        string requirements
        string structure
        string seo_title
    }
    PROGRAM_COURSE {
        guid program_id PK,FK
        guid course_id PK,FK
        int sequence
        bool is_required
    }
    PROGRAM_OFFERING {
        guid id PK
        guid program_id FK
        guid location_id FK
        datetimeoffset starts_at
        datetimeoffset ends_at
        decimal price
        string currency
        int capacity
        string status
    }
    CAREER_PATH {
        guid id PK
        string slug UK
        string status
        bool is_deleted
    }
    CAREER_PATH_TRANSLATION {
        guid career_path_id PK,FK
        string language_code PK
        string title
        string overview
        string recommended_skills_json
        string journey_json
    }
    CAREER_PATH_PROGRAM {
        guid career_path_id PK,FK
        guid program_id PK,FK
        int sort_order
    }
    CAREER_PATH_COURSE {
        guid career_path_id PK,FK
        guid course_id PK,FK
        int sort_order
    }
    INSTRUCTOR_PROFILE {
        guid user_id PK
    }
    MEDIA_ASSET {
        guid id PK
    }

    COURSE_CATEGORY ||--o{ COURSE : classifies
    MEDIA_ASSET o|--o{ COURSE : illustrates
    COURSE ||--o{ COURSE_TRANSLATION : localizes
    COURSE ||--o{ COURSE_INSTRUCTOR : assigns
    INSTRUCTOR_PROFILE ||--o{ COURSE_INSTRUCTOR : teaches
    COURSE ||--o{ COURSE_OFFERING : schedules
    TRAINING_LOCATION ||--o{ COURSE_OFFERING : hosts
    COURSE_OFFERING ||--o{ COURSE_SESSION : contains
    INSTRUCTOR_PROFILE ||--o{ COURSE_SESSION : delivers
    TRAINING_LOCATION ||--o{ COURSE_SESSION : hosts
    PROGRAM_CATEGORY ||--o{ PROGRAM : classifies
    MEDIA_ASSET o|--o{ PROGRAM : illustrates
    PROGRAM ||--o{ PROGRAM_TRANSLATION : localizes
    PROGRAM ||--o{ PROGRAM_COURSE : contains
    COURSE ||--o{ PROGRAM_COURSE : participates_in
    PROGRAM ||--o{ PROGRAM_OFFERING : schedules
    TRAINING_LOCATION ||--o{ PROGRAM_OFFERING : hosts
    CAREER_PATH ||--o{ CAREER_PATH_TRANSLATION : localizes
    CAREER_PATH ||--o{ CAREER_PATH_PROGRAM : recommends
    PROGRAM ||--o{ CAREER_PATH_PROGRAM : appears_in
    CAREER_PATH ||--o{ CAREER_PATH_COURSE : recommends
    COURSE ||--o{ CAREER_PATH_COURSE : appears_in
```

> **⚠ BUSINESS RULE TO CONFIRM:** Determine whether related courses and programs are manually curated or calculated. The two career-path relationship tables are valid only when manual curation is required.

## 3. Applications, payments, and enrollment

```mermaid
erDiagram
    APPLICATION_USER {
        guid id PK
    }
    COURSE_OFFERING {
        guid id PK
    }
    PROGRAM_OFFERING {
        guid id PK
    }
    APPLICATION {
        guid id PK
        guid applicant_id FK
        guid course_offering_id FK
        guid program_offering_id FK
        string status
        datetimeoffset submitted_at
        string correlation_id
    }
    APPLICATION_STATUS_HISTORY {
        guid id PK
        guid application_id FK
        guid changed_by_user_id FK
        string from_status
        string to_status
        string reason
        datetimeoffset changed_at
    }
    PAYMENT {
        guid id PK
        guid application_id FK
        guid user_id FK
        decimal amount
        string currency
        string status
        string idempotency_key UK
        string correlation_id
    }
    PAYMENT_ATTEMPT {
        guid id PK
        guid payment_id FK
        string provider
        string provider_reference UK
        string idempotency_key UK
        string status
        datetimeoffset initiated_at
    }
    PAYMENT_STATUS_HISTORY {
        guid id PK
        guid payment_id FK
        guid payment_attempt_id FK
        string status
        string source
        datetimeoffset changed_at
    }
    PAYMENT_WEBHOOK_RECEIPT {
        guid id PK
        string provider_event_id UK
        string payload_hash
        bool signature_valid
        string processing_status
        datetimeoffset received_at
    }
    ENROLLMENT {
        guid id PK
        guid application_id FK
        guid learner_id FK
        guid course_offering_id FK
        guid program_offering_id FK
        string status
        datetimeoffset enrolled_at
        datetimeoffset completed_at
        datetimeoffset cancelled_at
    }
    IDEMPOTENCY_RECORD {
        guid id PK
        string scope UK
        string key UK
        string request_hash
        string response_reference
        datetimeoffset expires_at
    }

    APPLICATION_USER ||--o{ APPLICATION : submits
    COURSE_OFFERING o|--o{ APPLICATION : targets
    PROGRAM_OFFERING o|--o{ APPLICATION : targets
    APPLICATION ||--|{ APPLICATION_STATUS_HISTORY : records
    APPLICATION_USER ||--o{ APPLICATION_STATUS_HISTORY : changes
    APPLICATION ||--o| PAYMENT : requires
    APPLICATION_USER ||--o{ PAYMENT : owns
    PAYMENT ||--o{ PAYMENT_ATTEMPT : attempts
    PAYMENT ||--|{ PAYMENT_STATUS_HISTORY : records
    PAYMENT_ATTEMPT o|--o{ PAYMENT_STATUS_HISTORY : causes
    APPLICATION ||--o| ENROLLMENT : produces
    APPLICATION_USER ||--o{ ENROLLMENT : receives
    COURSE_OFFERING o|--o{ ENROLLMENT : grants
    PROGRAM_OFFERING o|--o{ ENROLLMENT : grants
```

Required database checks:

- An `Application` targets exactly one of `CourseOffering` or `ProgramOffering`.
- An `Enrollment` targets exactly one of `CourseOffering` or `ProgramOffering`.
- Every `Application` is created with its initial `ApplicationStatusHistory` record in the same transaction. The initial record has no previous status.
- Every `Payment` is created with its initial `PaymentStatusHistory` record in the same transaction. The initial record does not require a `PaymentAttempt`.
- Provider event IDs, provider references, and protected-write idempotency keys are unique within their defined scope.
- Enrollment history is never physically deleted.

> **⚠ BUSINESS RULE TO CONFIRM:** Admission states, manual-review rules, payment timing, free offerings, deposits or instalments, capacity reservation, refunds, cancellations, re-enrollment, and program-to-course access.

## 4. LMS Core

```mermaid
erDiagram
    APPLICATION_USER {
        guid id PK
    }
    COURSE {
        guid id PK
    }
    COURSE_OFFERING {
        guid id PK
    }
    COURSE_SESSION {
        guid id PK
    }
    ENROLLMENT {
        guid id PK
    }
    MEDIA_ASSET {
        guid id PK
    }
    COURSE_MATERIAL {
        guid id PK
        guid course_id FK
        guid session_id FK
        guid media_asset_id FK
        string material_type
        int sort_order
        string visibility
    }
    ATTENDANCE {
        guid id PK
        guid session_id FK
        guid enrollment_id FK
        guid marked_by_user_id FK
        string status
        datetimeoffset marked_at
    }
    ASSIGNMENT {
        guid id PK
        guid course_offering_id FK
        guid session_id FK
        string title
        string instructions
        datetimeoffset due_at
        string publishing_status
    }
    ASSIGNMENT_SUBMISSION {
        guid id PK
        guid assignment_id FK
        guid enrollment_id FK
        string submission_text
        string status
        datetimeoffset submitted_at
    }
    SUBMISSION_ATTACHMENT {
        guid submission_id PK,FK
        guid media_asset_id PK,FK
    }
    MATERIAL_PROGRESS {
        guid enrollment_id PK,FK
        guid course_material_id PK,FK
        datetimeoffset accessed_at
        datetimeoffset completed_at
    }
    SESSION_PROGRESS {
        guid enrollment_id PK,FK
        guid course_session_id PK,FK
        string status
        datetimeoffset completed_at
    }
    COURSE_PROGRESS {
        guid enrollment_id PK,FK
        decimal percentage
        string status
        int calculation_version
        datetimeoffset calculated_at
    }

    COURSE ||--o{ COURSE_MATERIAL : contains
    COURSE_SESSION o|--o{ COURSE_MATERIAL : scopes
    MEDIA_ASSET o|--o{ COURSE_MATERIAL : stores
    COURSE_SESSION ||--o{ ATTENDANCE : records
    ENROLLMENT ||--o{ ATTENDANCE : receives
    APPLICATION_USER ||--o{ ATTENDANCE : marks
    COURSE_OFFERING ||--o{ ASSIGNMENT : contains
    COURSE_SESSION o|--o{ ASSIGNMENT : scopes
    ASSIGNMENT ||--o{ ASSIGNMENT_SUBMISSION : receives
    ENROLLMENT ||--o{ ASSIGNMENT_SUBMISSION : submits
    ASSIGNMENT_SUBMISSION ||--o{ SUBMISSION_ATTACHMENT : attaches
    MEDIA_ASSET ||--o{ SUBMISSION_ATTACHMENT : references
    ENROLLMENT ||--o{ MATERIAL_PROGRESS : tracks
    COURSE_MATERIAL ||--o{ MATERIAL_PROGRESS : measures
    ENROLLMENT ||--o{ SESSION_PROGRESS : tracks
    COURSE_SESSION ||--o{ SESSION_PROGRESS : measures
    ENROLLMENT ||--o| COURSE_PROGRESS : summarizes
```

> **⚠ BUSINESS RULE TO CONFIRM:** Attendance correction, assignment resubmission, feedback or scoring, material-completion behavior, and the exact progress formula.

## 5. Operations, CMS, notifications, and reliability

```mermaid
erDiagram
    APPLICATION_USER {
        guid id PK
    }
    INSTRUCTOR_PROFILE {
        guid user_id PK
    }
    COURSE_OFFERING {
        guid id PK
    }
    PROGRAM_OFFERING {
        guid id PK
    }
    INSTRUCTOR_COMPENSATION {
        guid id PK
        guid instructor_id FK
        guid course_offering_id FK
        guid program_offering_id FK
        decimal amount
        string currency
        string status
    }
    PAYOUT {
        guid id PK
        guid instructor_id FK
        decimal total
        string currency
        string status
        datetimeoffset approved_at
        datetimeoffset paid_at
    }
    PAYOUT_LINE_ITEM {
        guid payout_id PK,FK
        guid compensation_id PK,FK
    }
    MEDIA_ASSET {
        guid id PK
        string storage_key UK
        string content_type
        long size_bytes
        string checksum
        guid owner_user_id FK
        bool is_deleted
    }
    CONTENT_PAGE {
        guid id PK
        string page_key UK
        string slug UK
        string status
        datetimeoffset published_at
    }
    CONTENT_PAGE_TRANSLATION {
        guid page_id PK,FK
        string language_code PK
        string title
        string body
        string seo_title
        string seo_description
    }
    HOMEPAGE_SECTION {
        guid id PK
        string section_type
        int sort_order
        bool is_enabled
        string status
    }
    HOMEPAGE_SECTION_TRANSLATION {
        guid section_id PK,FK
        string language_code PK
        string content_json
    }
    PARTNER {
        guid id PK
        guid media_asset_id FK
        string website_url
        int sort_order
        bool is_published
    }
    ACCREDITATION {
        guid id PK
        guid media_asset_id FK
        string status
        datetimeoffset valid_from
        datetimeoffset valid_to
    }
    FAQ {
        guid id PK
        string category
        int sort_order
        bool is_published
    }
    FAQ_TRANSLATION {
        guid faq_id PK,FK
        string language_code PK
        string question
        string answer
    }
    TESTIMONIAL {
        guid id PK
        guid media_asset_id FK
        string attribution
        datetimeoffset consented_at
        bool is_published
    }
    SITE_SETTING {
        guid id PK
        string setting_key UK
        string public_value
    }
    NOTIFICATION_TEMPLATE {
        guid id PK
        string code UK
        string allowed_channels
        bool is_active
    }
    NOTIFICATION_TEMPLATE_TRANSLATION {
        guid template_id PK,FK
        string language_code PK
        string channel PK
        string subject
        string body
    }
    NOTIFICATION {
        guid id PK
        guid recipient_user_id FK
        guid template_id FK
        string event_type
        string data_json
        datetimeoffset read_at
    }
    NOTIFICATION_DELIVERY {
        guid id PK
        guid notification_id FK
        string channel
        string status
        int attempt_count
        string provider_reference
    }
    OUTBOX_MESSAGE {
        guid id PK
        string message_type
        string payload_json
        string status
        datetimeoffset occurred_at
        datetimeoffset processed_at
    }
    AUDIT_LOG {
        guid id PK
        guid actor_user_id FK
        string action
        string entity_type
        string entity_id
        string correlation_id
        string safe_changes_json
        datetimeoffset occurred_at
    }

    INSTRUCTOR_PROFILE ||--o{ INSTRUCTOR_COMPENSATION : earns
    COURSE_OFFERING o|--o{ INSTRUCTOR_COMPENSATION : generates
    PROGRAM_OFFERING o|--o{ INSTRUCTOR_COMPENSATION : generates
    INSTRUCTOR_PROFILE ||--o{ PAYOUT : receives
    PAYOUT ||--o{ PAYOUT_LINE_ITEM : contains
    INSTRUCTOR_COMPENSATION ||--o| PAYOUT_LINE_ITEM : settles
    APPLICATION_USER o|--o{ MEDIA_ASSET : owns
    CONTENT_PAGE ||--o{ CONTENT_PAGE_TRANSLATION : localizes
    HOMEPAGE_SECTION ||--o{ HOMEPAGE_SECTION_TRANSLATION : localizes
    MEDIA_ASSET o|--o{ PARTNER : illustrates
    MEDIA_ASSET o|--o{ ACCREDITATION : illustrates
    FAQ ||--o{ FAQ_TRANSLATION : localizes
    MEDIA_ASSET o|--o{ TESTIMONIAL : illustrates
    NOTIFICATION_TEMPLATE ||--o{ NOTIFICATION_TEMPLATE_TRANSLATION : localizes
    APPLICATION_USER ||--o{ NOTIFICATION : receives
    NOTIFICATION_TEMPLATE ||--o{ NOTIFICATION : creates
    NOTIFICATION ||--o{ NOTIFICATION_DELIVERY : delivers
    APPLICATION_USER o|--o{ AUDIT_LOG : performs
```

`OutboxMessage` is intentionally independent in the conceptual ERD because it records events from multiple aggregates. Its payload must not contain secrets or unnecessary personal data.

> **⚠ BUSINESS RULE TO CONFIRM:** Compensation approval, payout workflow, editable CMS fields, content approval, notification channels, SMS provider, opt-out rules, and retention policies.

## Relationship summary

- One-to-one: user to learner profile, user to instructor profile, enrollment to course-progress summary.
- One-to-many: course to offerings, offering to sessions, application to one-or-more status-history records, payment to one-or-more status-history records, payment to attempts, and notification to delivery attempts.
- Many-to-many through explicit join entities: users to roles, roles to permissions, programs to courses, career paths to programs/courses, courses to instructors, submissions to media assets.
- Exclusive alternative targets: application, enrollment, and compensation target rules require SQL check constraints rather than polymorphic foreign keys.

## Implementation gate

Do not generate EF Core entities or migrations from this document until the listed business rules are reviewed and the physical schema, indexes, delete behaviors, concurrency strategy, and ownership boundaries are approved.
