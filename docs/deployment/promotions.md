Promotion workflow

- Base controls images and VERSION; overlays do not set images. CI/workflow injects VERSION (commit SHA) into base before render.
- Render and validate:
  - kustomize build deploy/k8s/overlays/staging | kubeconform -strict -summary -
  - kustomize build deploy/k8s/overlays/prod    | kubeconform -strict -summary -
- Hosts and TLS secrets are set per overlay patch; HSTS annotations only in prod.
- Use immutable image tags (commit SHA) for promotion.

Deployments

- Staging: run “Deploy to Staging (GKE)” with sha set to the commit SHA that passed CI. The job authenticates via GCP Workload Identity Federation (OIDC), renders overlays/staging with VERSION=sha, runs kubectl diff, applies with server-side apply and prune (label app=assets), then waits for rollout.
- Production: run “Deploy to Prod (GKE)” with the same sha used in staging. Environment protection provides manual approval. Same render/diff/apply/rollout flow.

Rollback

- kubectl -n assets-staging rollout undo deployment assets-api
- kubectl -n assets-staging rollout undo deployment assets-worker
- kubectl -n assets-prod rollout undo deployment assets-api
- kubectl -n assets-prod rollout undo deployment assets-worker
