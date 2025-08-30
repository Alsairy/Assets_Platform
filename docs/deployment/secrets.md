# Secrets

## Source of truth
- Google Secret Manager (GSM) per environment.

## Required keys
- OIDC__AUTHORITY
- Flowable: Flowable__BaseUrl, Flowable__Username, Flowable__Password
- ConnectionStrings__Default
- GoogleCloud: GoogleCloud__ProjectId
- GCS: GCS__BucketName
- Google Vision: GOOGLE_APPLICATION_CREDENTIALS (mounted as file) or service account via Workload Identity

## Sync to Kubernetes
- Use CSI driver for GSM or a pipeline that renders K8s Secrets from GSM.
- Never commit secrets to git.
- Prefer Workload Identity over key files; if key is needed, mount as projected volume.

## Rotation
- Rotate credentials quarterly or on incident.
- Use separate service accounts per environment.
