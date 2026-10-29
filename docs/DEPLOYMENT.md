# Production deployment

This guide describes the production-safe deployment contract. It intentionally does not select a hosting vendor or put secrets in repository configuration.

## Required configuration

Provide these values through GitHub environment secrets, Azure Key Vault, or an equivalent secret manager:

- `ConnectionStrings__DefaultConnection`: encrypted production SQL Server/Azure SQL connection string.
- `Jwt__Key`: Base64-encoded output of at least 32 cryptographically random bytes.
- `AllowedHosts`: semicolon-separated production API host names; wildcards are rejected.
- `Cors__AllowedOrigins__0` and following indexes: explicit HTTPS web origins.
- `API_IMAGE` and `WEB_IMAGE`: immutable image tags or digests.

The API fails during Production startup if required secrets are missing, CORS is insecure, hosts are wildcarded, HTTPS redirection is disabled, demo seeding is enabled, or automatic migrations are enabled.

`appsettings.Development.json`, `appsettings.Testing.json`, and `appsettings.Production.json` keep environment behavior separate. None contains a database password or JWT signing key.

## Migration gate

Production never calls `Database.Migrate()` on startup. Database changes are a reviewed deployment stage:

1. Run `pwsh ./scripts/build-deployment-artifacts.ps1` against the release commit.
2. Confirm `has-pending-model-changes` succeeds.
3. Review `artifacts/sql/andalusia-academy-migrations.sql` and its printed SHA-256 hash.
4. Back up the production database and verify restore procedures.
5. Apply the idempotent SQL artifact once using the deployment identity.
6. Verify `/health/ready` against the new schema.
7. Deploy the immutable API and web image tags.

The CI backend job generates the same SQL artifact. Its separate acceptance job starts from an empty SQL Server database and executes the full visitor -> application -> approval -> payment -> enrollment journey.

Rollback the application by redeploying the preceding immutable image. Database rollback is not automatic: use a separately reviewed rollback script only when a migration is proven reversible and no newer production data would be lost.

## Container deployment

Copy `deploy/production.env.example` to a secure deployment environment, populate it outside Git, and run:

```powershell
docker compose --env-file <secure-environment-file> -f docker-compose.production.yml up -d
```

The frontend image receives `VITE_API_BASE_URL` at image build time. Set it to the public HTTPS API URL when the web and API are served from different origins.

Terminate TLS at the approved reverse proxy or managed ingress and preserve `X-Forwarded-Proto`. The Compose template enables ASP.NET Core forwarded-header handling.

## Health and monitoring

- `/health/live`: process liveness only; suitable for restart decisions.
- `/health/ready`: SQL Server connectivity and notification-outbox backlog; suitable for traffic admission.
- `/health`: backwards-compatible aggregate endpoint.

Production logs are structured JSON with UTC timestamps and correlation scopes. Every response includes `X-Correlation-ID`; clients may supply a safe value for cross-system tracing. Unhandled errors log that identifier without request bodies, tokens, card information, or secrets.

Notification readiness becomes degraded when the pending queue is old or large and unhealthy after repeated delivery attempts. Override `Notifications__Health__*` only from reviewed environment configuration.

## Performance and acceptance

Run the load smoke test against a disposable or staging environment:

```powershell
docker run --rm --network host -v "${PWD}/tests/load:/scripts:ro" -e BASE_URL=http://localhost:8080 grafana/k6:latest run /scripts/platform-smoke.js
```

The scheduled `Performance smoke test` workflow enforces a failed-request rate below 1%, p95 below 750 ms, p99 below 1.5 seconds, and successful health/catalog checks. Tune thresholds only with measured evidence.
