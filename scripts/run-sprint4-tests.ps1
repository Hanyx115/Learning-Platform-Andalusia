[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$envPath = Join-Path $repoRoot '.env'

if (-not (Test-Path -LiteralPath $envPath)) {
    throw "Missing $envPath. Copy .env.example to .env and set MSSQL_SA_PASSWORD first."
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

$sqlPort = $settings['SQLSERVER_PORT']
if ([string]::IsNullOrWhiteSpace($sqlPort)) {
    $sqlPort = '1433'
}

$env:ANDALUSIA_TEST_CONNECTION_STRING =
    "Server=localhost,$sqlPort;Database=AndalusiaAcademyIntegrationTests;User Id=sa;Password=$sqlPassword;TrustServerCertificate=True;Encrypt=False"
$env:DOTNET_CLI_HOME = Join-Path (Split-Path -Parent $repoRoot) '.dotnet-cli'
$env:DOTNET_NOLOGO = '1'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

Push-Location $repoRoot
try {
    docker compose up -d --wait sqlserver
    if ($LASTEXITCODE -ne 0) { throw 'Docker SQL Server startup failed.' }

    dotnet restore AndalusiaAcademy.sln
    if ($LASTEXITCODE -ne 0) { throw 'dotnet restore failed.' }

    dotnet build AndalusiaAcademy.sln --configuration Release --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet build failed.' }

    dotnet test AndalusiaAcademy.sln --configuration Release --no-build --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'dotnet test failed.' }

    Push-Location (Join-Path $repoRoot 'frontend')
    try {
        npm run lint
        if ($LASTEXITCODE -ne 0) { throw 'frontend lint failed.' }

        npm test -- --run
        if ($LASTEXITCODE -ne 0) { throw 'frontend tests failed.' }

        npm run build
        if ($LASTEXITCODE -ne 0) { throw 'frontend build failed.' }
    }
    finally {
        Pop-Location
    }
}
finally {
    Remove-Item Env:ANDALUSIA_TEST_CONNECTION_STRING -ErrorAction SilentlyContinue
    Pop-Location
}
