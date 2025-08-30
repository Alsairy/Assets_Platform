# Environments

## Dev (local)
- Docker Compose: db, keycloak, flowable, api.
- Ports: API 8080, Keycloak 8081, Flowable 8082.
- OIDC__AUTHORITY=http://keycloak:8080/realms/moe

## Staging
- GKE Autopilot or GKE standard.
- Cloud SQL Postgres, external Keycloak or org IdP.
- Google Secret Manager for secrets, Workload Identity.
- GCS bucket for document storage, Vision API enabled.
- Flowable: managed external or in-cluster (optional).

## Prod
- Regional GKE, multi-zone nodes.
- Cloud SQL with HA + PITR.
- Private GKE nodes + Cloud NAT; WAF/Cloud Armor on Ingress.
- HPA on API and Worker; PodDisruptionBudgets.
- Centralized logging/metrics; SLO dashboards + alerts.

## Services and URLs
- API: /health, /health/ready, /swagger
- Worker: background processing (no service)
- Keycloak: external (preferred) or staging-only in-cluster
- Flowable: external (preferred) or dedicated namespace
