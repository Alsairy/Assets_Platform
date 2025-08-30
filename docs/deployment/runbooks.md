# Runbooks

- Cloud SQL PITR: scale API/worker to 0, restore into new instance, update CONNECTION_STRING, run migrations, scale up, verify.
- Worker backlog drain: scale to 0, resume with 1 replica, monitor job statuses.
- Flowable stuck workflows: check management endpoint, retry or move to error state.
- Keycloak outage: tokens cached briefly; degrade reads; disable strict audience in emergencies.
