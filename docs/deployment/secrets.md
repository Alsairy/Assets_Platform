# Secrets and configuration

ConfigMap (non-sensitive):
- OIDC_AUTHORITY
- FLOWABLE_BASEURL
- OCR_PROVIDER
- OCR_CONFIDENCE_THRESHOLD
- GOOGLE_PROJECT_ID
- GCS_BUCKET

Secret (sensitive):
- CONNECTION_STRING
- FLOWABLE_USERNAME
- FLOWABLE_PASSWORD

Prefer Workload Identity instead of key files for Google SDKs.
