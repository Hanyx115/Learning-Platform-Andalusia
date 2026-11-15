param(
    [string]$ContainerName = "learning-platform-andalusia-sqlserver-1",
    [string]$DatabaseName = "AndalusiaAcademy",
    [string]$OutputDirectory = "tmp/sprint4-report"
)

$ErrorActionPreference = "Stop"

New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null

$passwordLine = docker inspect $ContainerName --format '{{range .Config.Env}}{{println .}}{{end}}' |
    Where-Object { $_ -like 'MSSQL_SA_PASSWORD=*' } |
    Select-Object -First 1

if (-not $passwordLine) {
    throw "MSSQL_SA_PASSWORD was not found in the SQL Server container."
}

$databasePassword = $passwordLine.Substring('MSSQL_SA_PASSWORD='.Length)

function Invoke-DatabaseJsonQuery {
    param(
        [Parameter(Mandatory)]
        [string]$Query,

        [Parameter(Mandatory)]
        [string]$OutputPath
    )

    $sqlcmdArguments = @(
        '/opt/mssql-tools18/bin/sqlcmd',
        '-C', '-S', 'localhost', '-U', 'sa', '-P', $databasePassword,
        '-d', $DatabaseName, '-y', '0', '-w', '65535', '-Q', $Query
    )
    $queryOutput = & docker exec $ContainerName @sqlcmdArguments

    if ($LASTEXITCODE -ne 0) {
        throw "The database metadata query failed."
    }

    $jsonStart = [Array]::FindIndex(
        [string[]]$queryOutput,
        [Predicate[string]] { param($line) $line.TrimStart().StartsWith("[") }
    )

    if ($jsonStart -lt 0) {
        throw "The database metadata query did not return JSON."
    }

    $json = ($queryOutput[$jsonStart..($queryOutput.Count - 1)] -join "").Trim()
    $null = $json | ConvertFrom-Json
    [System.IO.File]::WriteAllText(
        (Join-Path (Get-Location) $OutputPath),
        $json,
        [System.Text.UTF8Encoding]::new($false)
    )
}

$columnQuery = @"
SET NOCOUNT ON;
SELECT s.name AS [schema], t.name AS [table], c.column_id AS ordinal,
       c.name AS [column], ty.name AS [type], c.max_length, c.precision,
       c.scale, c.is_nullable, c.is_identity,
       CASE WHEN pk.column_id IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS is_primary_key,
       CASE WHEN uq.column_id IS NULL THEN CAST(0 AS bit) ELSE CAST(1 AS bit) END AS is_unique
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN (
    SELECT ic.object_id, ic.column_id
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.is_primary_key = 1
) pk ON pk.object_id = c.object_id AND pk.column_id = c.column_id
LEFT JOIN (
    SELECT DISTINCT ic.object_id, ic.column_id
    FROM sys.indexes i
    JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.is_unique = 1 AND i.is_primary_key = 0
) uq ON uq.object_id = c.object_id AND uq.column_id = c.column_id
WHERE t.is_ms_shipped = 0 AND t.name <> '__EFMigrationsHistory'
ORDER BY s.name, t.name, c.column_id
FOR JSON PATH;
"@

$foreignKeyQuery = @"
SET NOCOUNT ON;
SELECT sch1.name AS [from_schema], tab1.name AS [from_table],
       col1.name AS [from_column], sch2.name AS [to_schema],
       tab2.name AS [to_table], col2.name AS [to_column],
       fk.name AS [constraint]
FROM sys.foreign_key_columns fkc
JOIN sys.foreign_keys fk ON fk.object_id = fkc.constraint_object_id
JOIN sys.tables tab1 ON tab1.object_id = fkc.parent_object_id
JOIN sys.schemas sch1 ON sch1.schema_id = tab1.schema_id
JOIN sys.columns col1 ON col1.object_id = fkc.parent_object_id AND col1.column_id = fkc.parent_column_id
JOIN sys.tables tab2 ON tab2.object_id = fkc.referenced_object_id
JOIN sys.schemas sch2 ON sch2.schema_id = tab2.schema_id
JOIN sys.columns col2 ON col2.object_id = fkc.referenced_object_id AND col2.column_id = fkc.referenced_column_id
WHERE tab1.is_ms_shipped = 0
ORDER BY sch1.name, tab1.name, col1.name
FOR JSON PATH;
"@

Invoke-DatabaseJsonQuery -Query $columnQuery -OutputPath (Join-Path $OutputDirectory "schema-columns.json")
Invoke-DatabaseJsonQuery -Query $foreignKeyQuery -OutputPath (Join-Path $OutputDirectory "schema-fks.json")

Write-Host "Exported database metadata to $OutputDirectory."
