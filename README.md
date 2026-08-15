# Gastro API — Kubernetes Deployment

Production deployment of the Gastro API on a lightweight Kubernetes cluster based on **K3s**.

The repository contains the application containerization, Kubernetes manifests, production overlay, database resources, migrations, and Traefik ingress configuration required to run the API in production.

---

## 1. Architecture Overview

### High-level architecture

```text
                         Internet
                            |
                            v
                  gastro.opik.net:443
                            |
                            v
                    +---------------+
                    |    Traefik    |
                    |   Ingress     |
                    +-------+-------+
                            |
                            | HTTP :80
                            v
                  +-------------------+
                  |    Gastro API     |
                  |    Kubernetes     |
                  |    Deployment     |
                  +---------+---------+
                            |
                +-----------+-----------+
                |                       |
                v                       v
        +---------------+       +---------------+
        |  PostgreSQL   |       | Kubernetes    |
        |  StatefulSet  |       | Secrets /     |
        |               |       | ConfigMap     |
        +---------------+       +---------------+
````

### Main components

| Component           | Technology                       | Purpose                                        |
| ------------------- | -------------------------------- | ---------------------------------------------- |
| Kubernetes          | K3s                              | Lightweight production Kubernetes distribution |
| Ingress Controller  | Traefik 3.7.x                    | HTTP/HTTPS routing and TLS termination         |
| TLS                 | Let's Encrypt / ACME             | Automatic public certificate issuance          |
| Application         | ASP.NET Core / .NET 10           | Gastro API                                     |
| Container Registry  | GitHub Container Registry (GHCR) | Application image storage                      |
| Database            | PostgreSQL                       | Persistent application data                    |
| Deployment          | Kubernetes Deployment            | API workload management                        |
| Configuration       | ConfigMap + Secrets              | Runtime configuration                          |
| Database migrations | Kubernetes Job                   | Explicit EF Core migrations                    |
| Packaging           | Kustomize                        | Environment-specific Kubernetes configuration  |
| Storage             | K3s local-path                   | Persistent local Kubernetes volumes            |

---

## 2. Architectural Decisions

### 2.1 K3s

K3s was selected instead of a full Kubernetes distribution because the production environment is relatively small and requires a low operational footprint.

Benefits:

* Low resource consumption.
* Simple installation and maintenance.
* Built-in Kubernetes components.
* Suitable for small production clusters.
* Native integration with Traefik and local-path storage.

The cluster uses multiple nodes, with application workloads and Traefik managed by Kubernetes scheduling.

---

### 2.2 Traefik as the Ingress Controller

Traefik is used as the Kubernetes ingress controller.

The API is exposed through:

```text
https://gastro.opik.net
```

The Kubernetes Ingress uses:

```yaml
ingressClassName: traefik
```

Traefik handles:

* HTTP routing.
* HTTPS routing.
* TLS termination.
* Let's Encrypt certificate acquisition.
* HTTP → HTTPS redirection.
* Kubernetes Ingress discovery.

---

### 2.3 Let's Encrypt / ACME

TLS certificates are issued automatically through Let's Encrypt using the HTTP-01 challenge.

The configured resolver is:

```text
letsencrypt
```

The certificate storage is:

```text
/data/acme.json
```

Traefik's ACME data is backed by a Kubernetes PersistentVolumeClaim.

Current persistence configuration:

```yaml
persistence:
  enabled: true
  name: data
  accessMode: ReadWriteOnce
  size: 128Mi
  path: /data
  storageClass: local-path
```

This is important because the ACME account and certificates must survive a Traefik pod restart.

---

### 2.4 HTTP → HTTPS Redirection

HTTP traffic is redirected to HTTPS by Traefik.

The effective Traefik configuration contains:

```text
--entryPoints.web.http.redirections.entryPoint.to=websecure
--entryPoints.web.http.redirections.entryPoint.scheme=https
--entryPoints.web.http.redirections.entryPoint.permanent=true
```

The expected behavior is:

```text
http://gastro.opik.net/health/live
        |
        | 308 Permanent Redirect
        v
https://gastro.opik.net/health/live
        |
        v
Healthy
```

The public HTTPS endpoint is port `443`.

Traefik's internal `websecure` entrypoint listens on container port `8443`, but this port must not appear in the public redirect URL.

---

### 2.5 Kubernetes Ingress

The API uses a standard Kubernetes `networking.k8s.io/v1` Ingress:

```yaml
spec:
  ingressClassName: traefik
```

Production hostname:

```text
gastro.opik.net
```

Backend service:

```text
gastro-api-service
```

The Service exposes the application through Kubernetes Service port `80`.

---

### 2.6 Application listens on port 8080

The ASP.NET Core application is explicitly configured to listen on:

```text
http://+:8080
```

through:

```yaml
ASPNETCORE_URLS: "http://+:8080"
```

External TLS termination is handled by Traefik.

Therefore:

```text
Internet
   |
   | HTTPS :443
   v
Traefik
   |
   | HTTP
   v
Gastro API :8080
```

The application itself does not terminate public TLS.

---

### 2.7 Container runs as a non-root user

The Docker image creates and uses an application-specific system user:

```dockerfile
USER appuser
```

This follows the principle of least privilege and avoids running ASP.NET Core as root inside the container.

The final image also installs:

```text
libgssapi-krb5-2
```

which is required by the application's runtime dependencies.

---

### 2.8 Explicit database migrations

The API container does **not** execute Entity Framework Core migrations during application startup.

The application configuration contains:

```yaml
ApplyMigrations: "false"
```

Database migrations are executed explicitly through a Kubernetes Job.

This prevents multiple API replicas from attempting migrations concurrently and makes schema changes an explicit deployment step.

---

### 2.9 Kustomize

The manifests are organized as:

```text
k8s/
├── base/
└── overlays/
    └── production/
```

The base contains reusable Kubernetes resources.

The production overlay contains environment-specific configuration.

This allows additional environments to be introduced without duplicating the entire Kubernetes configuration.

---

## 3. Repository Structure

```text
.
├── Dockerfile
├── k8s/
│   ├── base/
│   │   ├── configmap.yaml
│   │   ├── deployment.yaml
│   │   ├── https-redirect.yaml
│   │   ├── ingress-http.yaml
│   │   ├── ingress.yaml
│   │   ├── kustomization.yaml
│   │   ├── migration-job.yaml
│   │   ├── namespace.yaml
│   │   ├── postgres-service.yaml
│   │   ├── postgres-statefulset.yaml
│   │   ├── secrets.yaml
│   │   └── service.yaml
│   │
│   ├── overlays/
│   │   └── production/
│   │       ├── deployment-patch.yaml
│   │       ├── ingress-patch.yaml
│   │       └── kustomization.yaml
│   │
│   └── secrets/
│       └── gastro-api-secrets.example.env
│
└── ...
```

---

## 4. Prerequisites

The deployment environment requires:

* Linux.
* Git.
* K3s.
* `kubectl` access through `k3s kubectl`.
* Network access to the Kubernetes cluster.
* DNS control for `gastro.opik.net`.
* GitHub Container Registry access.
* PostgreSQL configuration and secrets.

DNS must resolve:

```text
gastro.opik.net -> <public-server-IP>
```

Required public ports:

```text
TCP 80
TCP 443
```

Port 80 is required for the Let's Encrypt HTTP-01 challenge and HTTP → HTTPS redirection.

---

## 5. Clone the Repository

```bash
git clone https://github.com/radixartem/gastro-api-k8s.git
cd gastro-api-k8s
```

Verify the branch:

```bash
git branch --show-current
```

Expected:

```text
main
```

---

## 6. Verify the Kubernetes Cluster

Check nodes:

```bash
sudo k3s kubectl get nodes -o wide
```

All production nodes should report:

```text
STATUS   Ready
```

Check all workloads:

```bash
sudo k3s kubectl get pods -A -o wide
```

---

## 7. Configure Application Secrets

Production secrets must not be committed to Git.

Use:

```text
k8s/secrets/gastro-api-secrets.example.env
```

as the template.

Verify Kubernetes Secrets:

```bash
sudo k3s kubectl -n gastro-api get secrets
```

Never commit:

* Database passwords.
* Connection strings containing credentials.
* API keys.
* Tokens.
* Private keys.
* Other production credentials.

---

## 8. Build the Container

Build the application image:

```bash
docker build \
  -t ghcr.io/radixartem/gastro-api-k8s:<TAG> \
  .
```

Recommended production tag:

```text
<git-commit-sha>
```

For example:

```text
ghcr.io/radixartem/gastro-api-k8s:62c22db
```

Immutable tags are preferred over `latest`.

---

## 9. Push the Image to GHCR

Authenticate with GitHub Container Registry:

```bash
docker login ghcr.io
```

Push:

```bash
docker push ghcr.io/radixartem/gastro-api-k8s:<TAG>
```

---

## 10. Configure the Kubernetes Image

Check the Deployment:

```bash
grep -n "image:" k8s/base/deployment.yaml
```

Example:

```yaml
image: ghcr.io/radixartem/gastro-api-k8s:<TAG>
imagePullPolicy: Always
```

The migration Job should use the same image:

```bash
grep -n "image:" k8s/base/migration-job.yaml
```

This guarantees that migrations and the running application use the same application version.

---

## 11. Validate Kubernetes Manifests

Render the production configuration:

```bash
sudo k3s kubectl kustomize k8s/overlays/production \
  > /tmp/gastro-api-production.yaml
```

Inspect it:

```bash
less /tmp/gastro-api-production.yaml
```

Server-side validation:

```bash
sudo k3s kubectl apply \
  --dry-run=server \
  -k k8s/overlays/production
```

---

## 12. Deploy the Application

Apply the production overlay:

```bash
sudo k3s kubectl apply -k k8s/overlays/production
```

Check resources:

```bash
sudo k3s kubectl -n gastro-api get all
```

---

## 13. Database Migrations

Database migrations are intentionally separated from application startup.

Check Jobs:

```bash
sudo k3s kubectl -n gastro-api get jobs
```

Inspect migration logs:

```bash
sudo k3s kubectl -n gastro-api logs job/<migration-job-name>
```

A migration must complete successfully before a schema-changing deployment is considered complete.

---

## 14. Verify the Deployment

Check Deployment:

```bash
sudo k3s kubectl -n gastro-api get deployment
```

Check Pods:

```bash
sudo k3s kubectl -n gastro-api get pods -o wide
```

Check Service:

```bash
sudo k3s kubectl -n gastro-api get svc
```

Check Ingress:

```bash
sudo k3s kubectl -n gastro-api get ingress
```

Expected hostname:

```text
gastro.opik.net
```

---

## 15. Health Check

The API exposes:

```text
/health/live
```

Test HTTPS:

```bash
curl -4 https://gastro.opik.net/health/live
```

Expected:

```text
Healthy
```

Test HTTP redirection:

```bash
curl -4 -I http://gastro.opik.net/health/live
```

Expected:

```text
HTTP/1.1 308 Permanent Redirect
Location: https://gastro.opik.net/health/live
```

Follow the redirect:

```bash
curl -4 -L http://gastro.opik.net/health/live
```

Expected:

```text
Healthy
```

---

## 16. Verify TLS

Inspect the certificate:

```bash
echo | openssl s_client \
  -connect gastro.opik.net:443 \
  -servername gastro.opik.net \
  2>/dev/null |
openssl x509 -noout \
  -subject \
  -issuer \
  -dates \
  -ext subjectAltName
```

Expected certificate information:

```text
subject=CN = gastro.opik.net
issuer=C = US, O = Let's Encrypt, ...
DNS:gastro.opik.net
```

The certificate must contain:

```text
DNS:gastro.opik.net
```

---

## 17. Traefik Configuration

K3s manages Traefik through a `HelmChart` and `HelmChartConfig`.

The K3s Traefik configuration is:

```text
/var/lib/rancher/k3s/server/manifests/traefik-config.yaml
```

Current configuration:

```yaml
apiVersion: helm.cattle.io/v1
kind: HelmChartConfig
metadata:
  name: traefik
  namespace: kube-system
spec:
  valuesContent: |-
    deployment:
      replicas: 1

    persistence:
      enabled: true
      name: data
      accessMode: ReadWriteOnce
      size: 128Mi
      path: /data
      storageClass: local-path

    podSecurityContext:
      fsGroup: 65532
      fsGroupChangePolicy: OnRootMismatch

    certificatesResolvers:
      letsencrypt:
        acme:
          email: praktikant@alderis.de
          storage: /data/acme.json
          keyType: EC256
          httpChallenge:
            entryPoint: web
```

After changing Traefik configuration:

```bash
sudo k3s kubectl -n kube-system rollout status \
  deployment/traefik \
  --timeout=180s
```

Inspect arguments:

```bash
sudo k3s kubectl -n kube-system get deployment traefik \
  -o jsonpath='{.spec.template.spec.containers[0].args}'
```

---

## 18. Verify Traefik ACME Persistence

Check the PVC:

```bash
sudo k3s kubectl -n kube-system get pvc traefik
```

Expected:

```text
STATUS   Bound
```

Check the Traefik pod:

```bash
sudo k3s kubectl -n kube-system get pods \
  -l app.kubernetes.io/name=traefik \
  -o wide
```

Check ACME storage:

```bash
sudo k3s kubectl -n kube-system exec deploy/traefik -- \
  sh -c 'ls -la /data && wc -c /data/acme.json'
```

A non-zero size confirms that Traefik has stored ACME state.

---

## 19. Traefik Service Ports

The Traefik Kubernetes Service exposes:

```text
web       :80
websecure :443
```

Check:

```bash
sudo k3s kubectl -n kube-system get svc traefik \
  -o jsonpath='{range .spec.ports[*]}{.name}{" port="}{.port}{" targetPort="}{.targetPort}{" nodePort="}{.nodePort}{"\n"}{end}'
```

Expected structure:

```text
web       port=80
websecure port=443
```

Internally, Traefik may use:

```text
web       :8000
websecure :8443
```

Those are container ports and should not be exposed as public redirect destinations.

---

## 20. Troubleshooting

### API pod is not Ready

```bash
sudo k3s kubectl -n gastro-api get pods
```

Inspect:

```bash
sudo k3s kubectl -n gastro-api describe pod <pod-name>
```

Logs:

```bash
sudo k3s kubectl -n gastro-api logs <pod-name>
```

---

### Check API endpoints

```bash
sudo k3s kubectl -n gastro-api get endpoints
```

or:

```bash
sudo k3s kubectl -n gastro-api get endpointslices
```

---

### Traefik logs

```bash
sudo k3s kubectl -n kube-system logs deploy/traefik
```

Follow logs:

```bash
sudo k3s kubectl -n kube-system logs -f deploy/traefik
```

Search for ACME events:

```bash
sudo k3s kubectl -n kube-system logs deploy/traefik |
  grep -Ei 'acme|letsencrypt|certificate|gastro.opik.net'
```

Successful certificate issuance includes messages similar to:

```text
The server validated our request.
Validations succeeded; requesting certificates.
Server responded with a certificate.
```

---

### Ingress troubleshooting

```bash
sudo k3s kubectl -n gastro-api describe ingress gastro-api-ingress
```

Check:

```bash
sudo k3s kubectl -n gastro-api get ingress
```

Expected class:

```text
traefik
```

---

### Incorrect HTTP redirect

If this command:

```bash
curl -4 -I http://gastro.opik.net/health/live
```

returns:

```text
Location: https://gastro.opik.net:8443/health/live
```

then the public redirect is incorrect.

Port `8443` is Traefik's internal `websecure` container port.

The public URL must be:

```text
https://gastro.opik.net/health/live
```

Verify the Service:

```bash
sudo k3s kubectl -n kube-system get svc traefik \
  -o jsonpath='{range .spec.ports[*]}{.name}{" port="}{.port}{" targetPort="}{.targetPort}{"\n"}{end}'
```

The Service must expose:

```text
443 -> websecure
```

---

## 21. Git-Based Deployment Workflow

GitHub is the source of truth for application and Kubernetes configuration.

Recommended workflow:

```text
Developer
   |
   v
Git commit
   |
   v
GitHub main
   |
   v
Build container
   |
   v
GHCR
   |
   v
Update Kubernetes image tag
   |
   v
Kustomize production deployment
   |
   v
K3s cluster
```

Before changing a production server:

```bash
git status
git pull --ff-only
```

After changes:

```bash
git status
git diff
```

Commit:

```bash
git add <files>
git commit -m "Describe the change"
git push origin main
```

Then synchronize the production server:

```bash
git pull --ff-only
```

Avoid undocumented manual changes directly on the production host.

---

## 22. Production Deployment Procedure

A typical deployment should follow this sequence:

### Step 1 — Synchronize Git

```bash
git pull --ff-only
```

### Step 2 — Build the image

```bash
docker build \
  -t ghcr.io/radixartem/gastro-api-k8s:<TAG> \
  .
```

### Step 3 — Push the image

```bash
docker push ghcr.io/radixartem/gastro-api-k8s:<TAG>
```

### Step 4 — Update Kubernetes manifests

Update the application image in:

```text
k8s/base/deployment.yaml
k8s/base/migration-job.yaml
```

Use the same immutable image tag.

### Step 5 — Validate

```bash
sudo k3s kubectl apply \
  --dry-run=server \
  -k k8s/overlays/production
```

### Step 6 — Deploy

```bash
sudo k3s kubectl apply -k k8s/overlays/production
```

### Step 7 — Verify migration

```bash
sudo k3s kubectl -n gastro-api get jobs
```

### Step 8 — Verify rollout

```bash
sudo k3s kubectl -n gastro-api rollout status \
  deployment/gastro-api \
  --timeout=180s
```

### Step 9 — Verify health

```bash
curl -4 https://gastro.opik.net/health/live
```

Expected:

```text
Healthy
```

### Step 10 — Verify Git

```bash
git status
```

Expected:

```text
nothing to commit, working tree clean
```

---

## 23. Deployment Checklist

### Before deployment

* [ ] Git working tree is clean.
* [ ] Production branch is synchronized with GitHub.
* [ ] Container image has been built.
* [ ] Image has been pushed to GHCR.
* [ ] Image tag is immutable.
* [ ] Secrets are configured.
* [ ] Kubernetes manifests pass validation.
* [ ] DNS points to the production public IP.
* [ ] TCP 80 and 443 are reachable.

### During deployment

* [ ] Apply production Kustomize overlay.
* [ ] Run/verify database migrations.
* [ ] Wait for Deployment rollout.
* [ ] Check API pods.
* [ ] Check Service endpoints.
* [ ] Check Ingress.
* [ ] Check Traefik.

### After deployment

* [ ] HTTPS health endpoint returns `Healthy`.
* [ ] HTTP redirects to HTTPS.
* [ ] TLS certificate is valid.
* [ ] Certificate SAN contains `gastro.opik.net`.
* [ ] Traefik ACME state is persisted.
* [ ] PostgreSQL is healthy.
* [ ] Git repository is clean.

---

## 24. Useful Commands

### Kubernetes

```bash
sudo k3s kubectl get nodes -o wide
sudo k3s kubectl get pods -A -o wide
sudo k3s kubectl -n gastro-api get all
sudo k3s kubectl -n gastro-api get ingress
sudo k3s kubectl -n kube-system get pods -o wide
```

### Application logs

```bash
sudo k3s kubectl -n gastro-api logs deploy/gastro-api
```

### Traefik logs

```bash
sudo k3s kubectl -n kube-system logs deploy/traefik
```

### API rollout

```bash
sudo k3s kubectl -n gastro-api rollout status \
  deployment/gastro-api \
  --timeout=180s
```

### Restart API

```bash
sudo k3s kubectl -n gastro-api rollout restart \
  deployment/gastro-api
```

### Restart Traefik

```bash
sudo k3s kubectl -n kube-system rollout restart \
  deployment/traefik
```

### Git status

```bash
git status
git log --oneline -10
git remote -v
```

---

## 25. Operational Notes

### ACME storage

Do not delete the Traefik ACME volume unless certificate/account state is intentionally being reset.

The ACME file is:

```text
/data/acme.json
```

and is stored on the Traefik PVC.

---

### Secrets

Never commit production secrets to Git.

Use Kubernetes Secrets or an external secret-management system for production credentials.

---

### PostgreSQL

PostgreSQL uses persistent storage.

Persistent storage is **not a backup strategy**.

Production backups must be configured separately.

---

### Single Traefik replica

The current Traefik configuration uses:

```yaml
deployment:
  replicas: 1
```

This is appropriate for the current deployment model.

For higher availability, consider multiple Traefik replicas together with appropriate storage/network architecture.

---

### Local-path storage

K3s `local-path` storage is node-local.

It does not provide distributed replication.

It is suitable for the current deployment model but should not be treated as highly available storage.

For higher availability, consider:

* Longhorn.
* Ceph.
* NFS where appropriate.
* Another replicated storage platform.

---

## 26. Security Considerations

The deployment follows several baseline security practices:

* TLS is terminated at the ingress layer.
* HTTP is redirected to HTTPS.
* The application container runs as a non-root user.
* Kubernetes Secrets are used for sensitive configuration.
* Database migrations are separated from API startup.
* Container images should use immutable tags.
* Production configuration is version-controlled.
* Only required public ports should be exposed.

Recommended future improvements:

* Kubernetes NetworkPolicies.
* Pod Security Admission enforcement.
* Read-only root filesystem where compatible.
* Automated image vulnerability scanning.
* Centralized logging.
* Metrics and alerting.
* Automated PostgreSQL backups.
* External secret management.
* Multi-replica/high-availability Traefik.
* Distributed persistent storage.
* Disaster recovery procedures.

---

## 27. Source Repository

GitHub repository:

```text
https://github.com/radixartem/gastro-api-k8s
```

The `main` branch is the canonical source for the application and Kubernetes deployment configuration.

---

## 28. Production Endpoints

Main API:

```text
https://gastro.opik.net
```

Liveness:

```text
https://gastro.opik.net/health/live
```

Expected liveness response:

```text
Healthy
```

HTTP:

```text
http://gastro.opik.net
```

HTTP traffic is permanently redirected to HTTPS.

---

## 29. Architecture Summary

```text
                         +----------------+
                         |    GitHub      |
                         |     main       |
                         +-------+--------+
                                 |
                                 |
                         Application image
                                 |
                                 v
                         +---------------+
                         |      GHCR     |
                         +-------+-------+
                                 |
                                 |
                                 v
+----------------------------------------------------------------+
|                         K3s Cluster                            |
|                                                                |
|   +----------------+                                           |
|   |    Traefik     |                                           |
|   |                |                                           |
|   | HTTP :80       |                                           |
|   | HTTPS :443     |                                           |
|   +-------+--------+                                           |
|           |                                                    |
|           | HTTP                                               |
|           v                                                    |
|   +----------------+                                           |
|   |   Gastro API   |                                           |
|   |                |                                           |
|   | ASP.NET Core   |                                           |
|   | :8080          |                                           |
|   +-------+--------+                                           |
|           |                                                    |
|           v                                                    |
|   +----------------+                                           |
|   |   PostgreSQL   |                                           |
|   |  StatefulSet   |                                           |
|   +----------------+                                           |
|                                                                |
|   +----------------+                                           |
|   | Traefik PVC    |                                           |
|   | /data/acme.json|                                           |
|   +----------------+                                           |
|                                                                |
+----------------------------------------------------------------+
                                 |
                                 v
                         Let's Encrypt
                           ACME HTTP-01
```

The production architecture is based on the following principles:

1. **K3s** provides the lightweight Kubernetes platform.
2. **Traefik** provides ingress routing and TLS termination.
3. **Let's Encrypt ACME** provides automated public certificates.
4. **Persistent ACME storage** preserves certificate state across Traefik restarts.
5. **ASP.NET Core** listens internally on port `8080`.
6. **Kubernetes Service** provides stable application networking.
7. **Kustomize** separates reusable base manifests from production configuration.
8. **EF Core migrations** are executed explicitly rather than during API startup.
9. **GHCR** stores application container images.
10. **GitHub** is the source of truth for deployment configuration.
11. **PostgreSQL persistent storage** preserves database state.
12. **Backups remain a separate operational responsibility.**
13. **HTTP is permanently redirected to HTTPS.**
14. **Production images should use immutable version tags.**

---

## 30. Current Production Status

The production deployment has been verified with:

```bash
curl -4 https://gastro.opik.net/health/live
```

Response:

```text
Healthy
```

HTTP redirection:

```bash
curl -4 -I http://gastro.opik.net/health/live
```

Expected:

```text
HTTP/1.1 308 Permanent Redirect
Location: https://gastro.opik.net/health/live
```

TLS certificate:

```text
CN = gastro.opik.net
Issuer = Let's Encrypt
SAN = DNS:gastro.opik.net
```

The Traefik ACME state is persisted in:

```text
/data/acme.json
```

and backed by a Kubernetes PVC.

---

**Project:** Gastro API
**Repository:** `radixartem/gastro-api-k8s`
**Production hostname:** `gastro.opik.net`
**Kubernetes distribution:** K3s
**Ingress:** Traefik
**TLS:** Let's Encrypt / ACME
**Application runtime:** .NET 10 / ASP.NET Core

