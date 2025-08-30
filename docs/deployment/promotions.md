Promotion workflow

- Base controls images and VERSION; overlays do not set images. CI injects VERSION (commit SHA) into base only before render.
- Render and validate:
  - kustomize build deploy/k8s/overlays/dev     | kubeconform -strict -summary -
  - kustomize build deploy/k8s/overlays/staging | kubeconform -strict -summary -
  - kustomize build deploy/k8s/overlays/prod    | kubeconform -strict -summary -
- Hosts and TLS secrets are set per overlay patch; HSTS annotations only in prod.
- Use immutable image tags (commit SHA) for promotion.
