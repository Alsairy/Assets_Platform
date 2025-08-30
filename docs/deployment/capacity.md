Capacity and scaling

API
- Requests: cpu 250m, memory 512Mi
- HPA: CPU 70%, dev 1-2, staging 2-5, prod 3-10
- PDB: minAvailable 1

Worker
- Requests: cpu 200m, memory 256Mi
- HPA: CPU 70%, dev 1-2, staging 1-4, prod 2-8
- PDB: minAvailable 1

NetworkPolicy
- Default deny egress
- Allow ingress from ingress controller to API
- Allow egress 443 for API and worker
