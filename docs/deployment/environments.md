# Environments

## Dev
- Single-zone GKE
- Cloud SQL (non-HA)
- In-cluster Keycloak/Flowable acceptable
- Image pinning and resource requests/limits

## Staging
- Regional GKE
- Cloud SQL HA
- External SSO/Flowable preferred

## Prod
- Regional GKE
- Cloud SQL HA + PITR
- External SSO
- Hardened ingress with TLS/HSTS/WAF

## Notes
- Pin images (avoid :latest)
- Multi-replica API and separate worker Deployment
