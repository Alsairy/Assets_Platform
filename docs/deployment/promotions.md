Promotion workflow

- Overlays: deploy/k8s/overlays/dev, staging, prod
- CI injects VERSION from commit SHA into base images before rendering
- Render and dry-run apply:
  - kustomize build deploy/k8s/overlays/dev
  - kustomize build deploy/k8s/overlays/staging
  - kustomize build deploy/k8s/overlays/prod
- Hosts and TLS secrets are set per overlay patch
- Use immutable image tags (commit SHA) for promotion
