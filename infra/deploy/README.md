# Deployment

**Intentionally empty.**

No target environment, hosting model, registry, secret store or network topology
has been decided. Writing a plausible-looking Terraform module, Helm chart or
pipeline for an undecided platform would create something that looks reviewed
and is not, and someone would eventually run it.

What has to be decided before anything lands here:

1. Hosting model for the API — a single container service is the shape the
   architecture assumes (ADR-0001); Kubernetes needs an ADR demonstrating the
   need.
2. Managed PostgreSQL provider, version, backup schedule and a **rehearsed**
   restore. Restore is the part that is usually skipped and is the part that
   matters.
3. Secret storage and rotation, specifically for the decision-seed salt
   (ADR-0008) and tenant API keys.
4. TLS termination, allowed origins per tenant, and rate limiting at the edge.
5. An OTLP collector endpoint, plus which dashboards and alerts exist on day one
   (ADR-0011).
6. Image tagging and digest pinning, with a vulnerability-scanning policy.

Local development infrastructure lives in `infra/local/` and is not a deployment
artefact.
