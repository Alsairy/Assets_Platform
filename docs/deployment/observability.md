# Observability

- Logs: structured JSON via Serilog; include correlation IDs.
- Metrics: request rate/latency, worker jobs (started/succeeded/failed/low_confidence).
- Alerts: API 5xx, latency SLO breaches, worker failures > threshold, DB CPU > 80%, storage errors.
