# Dynamic Asset Management Platform — PRO Scaffold (World‑class Foundations)

This repository contains a **production-leaning implementation** aligned with your BRD:
- **.NET 8** backend (clean-ish layering) + **EF Core (PostgreSQL)**
- **React (Vite + TS)** frontend with dynamic rendering, map picker (Leaflet)
- **Flowable workflow adapter (REST)** — ready to point to your Flowable
- **Google Vision / Azure OCR adapters** (plug keys to enable)
- **RBAC** with role + region/city scoping and **field-level view/edit** enforcement
- **Documents** with OCR pipeline & versioning
- **Search & Reporting** endpoints (portfolio counts, asset listing with filters)
- **Serilog** logging • **K8s manifests** for **GKE (KSA)** • **GitLab CI** pipeline stub
- **Config via env**; secrets via K8s Secret/GCP Secret Manager suggested

> This is not a toy MVP — it’s a strong foundation you can deploy, audit, and extend to full enterprise. You’ll still need to point in production integrations (SSO/OIDC, Flowable URL, OCR keys, email/SMS gateways) and execute the hardening checklist below.

## Run (Docker)
```bash
docker compose up --build
# API: http://localhost:8080/swagger
# Web: http://localhost:5173
```

## Next Integrations (1–2 days each with access)
- **OIDC/JWT** (Google Cloud Identity / Keycloak) – swap auth to JwtBearer only
- **Flowable** – set `FLOWABLE__BASE_URL`, `FLOWABLE__USER`, `FLOWABLE__PASS`
- **OCR** – set `OCR__PROVIDER=Google|Azure` and keys
- **Mail/SMS** – add SendGrid/Twilio (or national gateways) credentials
- **Prometheus/Grafana** – add exporter & dashboards (charts provided in /ops)

## Structure
```
backend/
  App.Domain/           # Entities, enums
  App.Application/      # DTOs, policies
  App.Infrastructure/   # EF DbContext, adapters (Workflow/OCR)
  App.Api/              # Endpoints, auth, Serilog
frontend/web/           # React app
deploy/k8s/             # GKE manifests (api, web, ingress, secrets)
.gitlab-ci.yml          # Build/test/docker/deploy pipeline stub
```

## Hardening Checklist
- Enable **OIDC/JWT** (Google Identity / Keycloak) & remove header fallbacks
- Enforce **HTTPS** everywhere, set HSTS via ingress, configure **CSP** headers
- **KMS-encrypt** secrets (GCP Secret Manager) & mount via env
- **DB encryption at rest**, restricted network (private IP), **read replicas** for reports
- **Audit** every admin action (form schema changes, role/permission changes)
- **Field‑level encryption** for highly sensitive values if required
- **Backups & PITR** for DB and object storage; DR runbooks
- **SLA & Escalation** rules finalized with Flowable DMN tables
- **Pentest** and SAST/DAST in CI (add GitLab SAST template)
