# Environments and topology

- API: ASP.NET Core (port 8080)
- Worker: OcrJobWorker as separate Deployment
- Keycloak: external cluster/service
- Flowable REST: external or shared namespace
- DB: Cloud SQL for Postgres
- Object storage: GCS (bucket: assets-docs)

Config via ConfigMap; secrets via Secret or Workload Identity. Scale API with HPA on CPU/P95; Worker with queue depth or CPU.
