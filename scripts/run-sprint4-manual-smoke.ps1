[CmdletBinding()]
param(
    [string]$ApiBaseUrl = 'http://localhost:8080/api/v1'
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $repoRoot '.env'
$password = 'Demo!Pass2026'
$runId = [DateTimeOffset]::UtcNow.ToString('yyyyMMddHHmmss')
$learnerEmail = "sprint4.learner.$runId@demo.local"
$instructorEmail = "sprint4.instructor.$runId@demo.local"
$adminEmail = "sprint4.admin.$runId@demo.local"

function Invoke-JsonApi {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Path,
        [object]$Body,
        [string]$Token
    )

    $headers = @{}
    if ($Token) {
        $headers.Authorization = "Bearer $Token"
    }

    $parameters = @{
        Uri = "$ApiBaseUrl$Path"
        Method = $Method
        Headers = $headers
    }
    if ($null -ne $Body) {
        $parameters.ContentType = 'application/json'
        $parameters.Body = $Body | ConvertTo-Json -Depth 12
    }

    Invoke-RestMethod @parameters
}

function Register-DemoUser([string]$email, [string]$firstName, [string]$lastName) {
    Invoke-JsonApi -Method Post -Path '/auth/register' -Body @{
        email = $email
        password = $password
        firstName = $firstName
        lastName = $lastName
        phoneNumber = '+201000000000'
        countryCode = 'EG'
        preferredLanguage = 'en'
    }
}

function Login-DemoUser([string]$email) {
    Invoke-JsonApi -Method Post -Path '/auth/login' -Body @{
        email = $email
        password = $password
    }
}

if (-not (Test-Path -LiteralPath $envPath)) {
    throw "Missing $envPath."
}

$settings = @{}
Get-Content -LiteralPath $envPath | ForEach-Object {
    if ($_ -match '^\s*([^#=]+)=(.*)$') {
        $settings[$matches[1].Trim()] = $matches[2].Trim()
    }
}
$sqlPassword = $settings['MSSQL_SA_PASSWORD']
if ([string]::IsNullOrWhiteSpace($sqlPassword)) {
    throw 'MSSQL_SA_PASSWORD must be set in .env.'
}

$health = Invoke-WebRequest -Uri 'http://localhost:8080/health' -UseBasicParsing
if ($health.StatusCode -ne 200) {
    throw 'The API health endpoint is not healthy.'
}

Register-DemoUser $learnerEmail 'Sprint Four' 'Learner' | Out-Null
Register-DemoUser $instructorEmail 'Sprint Four' 'Instructor' | Out-Null
Register-DemoUser $adminEmail 'Sprint Four' 'Administrator' | Out-Null

$sqlContainer = docker compose -f (Join-Path $repoRoot 'docker-compose.yml') ps -q sqlserver
if ([string]::IsNullOrWhiteSpace($sqlContainer)) {
    throw 'The SQL Server container is not running.'
}

$roleSql = @"
SET NOCOUNT ON;
DECLARE @InstructorUserId uniqueidentifier = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = UPPER('$instructorEmail'));
DECLARE @AdminUserId uniqueidentifier = (SELECT Id FROM AspNetUsers WHERE NormalizedEmail = UPPER('$adminEmail'));
DECLARE @InstructorRoleId uniqueidentifier = (SELECT Id FROM AspNetRoles WHERE NormalizedName = 'INSTRUCTOR');
DECLARE @AdminRoleId uniqueidentifier = (SELECT Id FROM AspNetRoles WHERE NormalizedName = 'ADMIN');
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @InstructorUserId AND RoleId = @InstructorRoleId)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@InstructorUserId, @InstructorRoleId);
IF NOT EXISTS (SELECT 1 FROM AspNetUserRoles WHERE UserId = @AdminUserId AND RoleId = @AdminRoleId)
    INSERT INTO AspNetUserRoles (UserId, RoleId) VALUES (@AdminUserId, @AdminRoleId);
"@

docker exec $sqlContainer /opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P $sqlPassword -d AndalusiaAcademy -b -Q $roleSql
if ($LASTEXITCODE -ne 0) {
    throw 'Role preparation failed.'
}

$learnerAuth = Login-DemoUser $learnerEmail
$instructorAuth = Login-DemoUser $instructorEmail
$adminAuth = Login-DemoUser $adminEmail
$learnerToken = $learnerAuth.accessToken
$instructorToken = $instructorAuth.accessToken
$adminToken = $adminAuth.accessToken

$learnerProfile = Invoke-JsonApi -Method Get -Path '/profile' -Token $learnerToken
$instructorProfile = Invoke-JsonApi -Method Get -Path '/profile' -Token $instructorToken

$catalog = Invoke-JsonApi -Method Get -Path '/courses?Page=1&PageSize=1'
$course = @($catalog.items)[0]
if ($null -eq $course) {
    throw 'No seeded course is available for the Sprint 4 journey.'
}

$location = Invoke-JsonApi -Method Post -Path '/training-locations' -Token $adminToken -Body @{
    branchName = "Sprint 4 Branch $runId"
    roomName = 'Room 401'
    city = 'Cairo'
    address = 'Andalusia Academy demonstration location'
    mapUrl = 'https://maps.example.com/sprint4'
    capacity = 30
}

$now = [DateTimeOffset]::UtcNow
$offeringStart = $now.AddDays(7)
$offeringEnd = $now.AddDays(35)
$offering = Invoke-JsonApi -Method Post -Path "/courses/$($course.id)/offerings" -Token $adminToken -Body @{
    locationId = $location.id
    startsAtUtc = $offeringStart
    endsAtUtc = $offeringEnd
    applicationOpensAtUtc = $now.AddDays(-1)
    applicationClosesAtUtc = $now.AddDays(6)
    price = 250
    currency = 'EGP'
    capacity = 20
    status = 'Open'
}

$session = Invoke-JsonApi -Method Post -Path "/course-offerings/$($offering.id)/sessions" -Token $adminToken -Body @{
    locationId = $location.id
    instructorUserId = $instructorProfile.id
    title = 'Sprint 4 Demonstration Session'
    startsAtUtc = $offeringStart.AddDays(1)
    endsAtUtc = $offeringStart.AddDays(1).AddHours(2)
}

$material = Invoke-JsonApi -Method Post -Path "/courses/$($course.id)/materials" -Token $adminToken -Body @{
    courseSessionId = $session.id
    title = 'Sprint 4 Learning Guide'
    description = 'A demonstration resource available only to enrolled learners.'
    resourceUrl = 'https://example.com/sprint4-learning-guide'
    materialType = 'Link'
    sortOrder = 1
    visibility = 'Published'
}

$assignment = Invoke-JsonApi -Method Post -Path "/course-offerings/$($offering.id)/assignments" -Token $adminToken -Body @{
    title = 'Sprint 4 Practical Assignment'
    description = 'Submit a short reflection on the demonstration lesson.'
    dueAtUtc = $offeringStart.AddDays(5)
    maxScore = 100
}

$application = Invoke-JsonApi -Method Post -Path '/applications' -Token $learnerToken -Body @{
    courseOfferingId = $offering.id
    programOfferingId = $null
}
Invoke-JsonApi -Method Post -Path "/applications/$($application.id)/accept" -Token $adminToken | Out-Null
$payment = Invoke-JsonApi -Method Post -Path '/payments' -Token $learnerToken -Body @{
    applicationId = $application.id
}
$enrollment = $payment.enrollment

Invoke-JsonApi -Method Post -Path "/assignments/$($assignment.id)/submit" -Token $learnerToken -Body @{
    content = 'Sprint 4 learner submission completed during the manual end-to-end test.'
} | Out-Null

$roster = Invoke-JsonApi -Method Get -Path "/course-sessions/$($session.id)/attendance" -Token $instructorToken
if (-not (@($roster).Where({ $_.userId -eq $learnerProfile.id }))) {
    throw 'The enrolled learner did not appear in the instructor attendance roster.'
}
Invoke-JsonApi -Method Put -Path "/course-sessions/$($session.id)/attendance" -Token $instructorToken -Body @{
    entries = @(@{
        userId = $learnerProfile.id
        status = 'Present'
        note = 'Verified during the Sprint 4 manual test.'
    })
} | Out-Null

Invoke-JsonApi -Method Put -Path "/assignments/$($assignment.id)/submissions/$($learnerProfile.id)/grade" -Token $instructorToken -Body @{
    score = 92
    feedback = 'Manual Sprint 4 workflow verified.'
} | Out-Null

$enrollments = Invoke-JsonApi -Method Get -Path '/enrollments/me' -Token $learnerToken
$sessions = Invoke-JsonApi -Method Get -Path '/learner/upcoming-sessions' -Token $learnerToken
$materials = Invoke-JsonApi -Method Get -Path "/learner/enrollments/$($enrollment.id)/materials" -Token $learnerToken
$assignments = Invoke-JsonApi -Method Get -Path '/learner/assignments' -Token $learnerToken
$attendance = Invoke-JsonApi -Method Get -Path '/learner/attendance' -Token $learnerToken
$progress = Invoke-JsonApi -Method Get -Path '/learner/progress' -Token $learnerToken
$instructorSessions = Invoke-JsonApi -Method Get -Path '/instructor/sessions' -Token $instructorToken
$instructorOfferings = Invoke-JsonApi -Method Get -Path '/instructor/offerings' -Token $instructorToken
$instructorStudents = Invoke-JsonApi -Method Get -Path '/instructor/students' -Token $instructorToken
$adminEnrollments = Invoke-JsonApi -Method Get -Path '/enrollments' -Token $adminToken
$adminPayments = Invoke-JsonApi -Method Get -Path '/payments' -Token $adminToken

$unauthorizedResponse = Invoke-WebRequest -Uri "$ApiBaseUrl/training-locations" -Method Post `
    -Headers @{ Authorization = "Bearer $learnerToken" } -ContentType 'application/json' `
    -Body (@{ branchName='Blocked'; roomName='Blocked'; city='Cairo'; address='Blocked'; capacity=1 } | ConvertTo-Json) `
    -SkipHttpErrorCheck
if ($unauthorizedResponse.StatusCode -ne 403) {
    throw "Expected a learner location-management attempt to return 403, but received $($unauthorizedResponse.StatusCode)."
}

$checks = [ordered]@{
    Health = $health.StatusCode -eq 200
    LearnerEnrollment = @($enrollments).Where({ $_.id -eq $enrollment.id }).Count -eq 1
    LearnerSession = @($sessions).Where({ $_.sessionId -eq $session.id }).Count -eq 1
    LearnerMaterial = @($materials).Where({ $_.id -eq $material.id }).Count -eq 1
    LearnerAssignment = @($assignments).Where({ $_.assignmentId -eq $assignment.id -and $_.myScore -eq 92 }).Count -eq 1
    LearnerAttendance = @($attendance).Where({ $_.sessionId -eq $session.id -and $_.status -eq 'Present' }).Count -eq 1
    LearnerProgress = @($progress).Where({ $_.enrollmentId -eq $enrollment.id }).Count -eq 1
    InstructorSession = @($instructorSessions).Where({ $_.sessionId -eq $session.id }).Count -eq 1
    InstructorOffering = @($instructorOfferings).Where({ $_.courseOfferingId -eq $offering.id }).Count -eq 1
    InstructorStudent = @($instructorStudents).Where({ $_.userId -eq $learnerProfile.id }).Count -eq 1
    AdminEnrollmentView = @($adminEnrollments).Where({ $_.id -eq $enrollment.id }).Count -eq 1
    AdminPaymentView = @($adminPayments).Where({ $_.id -eq $payment.payment.id }).Count -eq 1
    UnauthorizedActionBlocked = $unauthorizedResponse.StatusCode -eq 403
}

$failed = @($checks.GetEnumerator() | Where-Object { -not $_.Value })
if ($failed.Count -gt 0) {
    throw "Sprint 4 manual checks failed: $($failed.Name -join ', ')"
}

[pscustomobject]@{
    RunId = $runId
    LearnerEmail = $learnerEmail
    InstructorEmail = $instructorEmail
    AdminEmail = $adminEmail
    CourseTitle = $course.title
    EnrollmentId = $enrollment.id
    OfferingId = $offering.id
    SessionId = $session.id
    AssignmentId = $assignment.id
    ChecksPassed = $checks.Count
} | Format-List
