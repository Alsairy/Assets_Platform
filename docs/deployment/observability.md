# Observability

## Metrics
- Request rate, latency (p50/p90/p99), error rate.
- OCR job counters: started, succeeded, failed, low-confidence.
- Flowable: REST 5xx, latency, task backlog.

## Logs
- Structured JSON logs with correlation IDs.
- Sensitive data excluded.

## Dashboards
- API SLO, Worker throughput and failure rate.
- Flowable engine health, task SLA breaches.

## Alerts
- API error rate above threshold.
- Worker failures or backlog > N for M minutes.
- Flowable REST unavailable.
