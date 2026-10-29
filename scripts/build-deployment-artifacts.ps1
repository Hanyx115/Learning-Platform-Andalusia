[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',
    [switch]$NoBuild
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$artifactDirectory = Join-Path $repoRoot 'artifacts/sql'
$migrationScript = Join-Path $artifactDirectory 'andalusia-academy-migrations.sql'

Push-Location $repoRoot
try {
    dotnet tool restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet tool restore failed.' }

    dotnet restore AndalusiaAcademy.sln
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    if (-not $NoBuild) {
        dotnet build AndalusiaAcademy.sln --configuration $Configuration --no-restore
        if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }
    }

    $commonArguments = @(
        '--project', 'src/AndalusiaAcademy.Infrastructure/AndalusiaAcademy.Infrastructure.csproj',
        '--startup-project', 'src/AndalusiaAcademy.API/AndalusiaAcademy.API.csproj',
        '--configuration', $Configuration
    )
    if ($NoBuild) {
        $commonArguments += '--no-build'
    }

    dotnet ef migrations has-pending-model-changes @commonArguments
    if ($LASTEXITCODE -ne 0) {
        throw 'The EF model has changes that are not represented by a migration.'
    }

    New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
    dotnet ef migrations script --idempotent @commonArguments --output $migrationScript
    if ($LASTEXITCODE -ne 0) { throw 'Migration script generation failed.' }

    $hash = Get-FileHash -LiteralPath $migrationScript -Algorithm SHA256
    Write-Host "Migration artifact: $migrationScript"
    Write-Host "SHA256: $($hash.Hash)"
}
finally {
    Pop-Location
}
