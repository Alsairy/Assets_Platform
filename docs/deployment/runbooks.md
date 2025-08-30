# Runbooks

## Rollback
- Keep previous image tag.
- kubectl rollout undo deployment api/worker
- Verify /health and application logs.

## Database
- Cloud SQL PITR: restore to new instance, update ConnectionStrings__Default via Secret, rollout.

## OCR backlog drain
- Scale worker up to handle backlog.
- Check queue by counting OcrJobs in Queued/Processing.
- Watch worker logs for failures/retries.

## Flowable issues
- If process start fails, API continues; investigate Flowable REST /service/management/engine.
- Re-deploy BPMN if missing; verify credentials and base URL.

## Keycloak outage
- API endpoints require auth; outage blocks requests.
- Validate IdP health; failover per organization policy.
