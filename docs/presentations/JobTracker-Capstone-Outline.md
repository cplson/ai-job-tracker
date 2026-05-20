# JobTracker Capstone Presentation — Outline & Speaker Notes

**Duration:** ~10 minutes (14 slides)  
**Focus:** CI/CD pipeline (Jenkins, security scanning, automated deploy)  
**Demo account:** `demo@jobtracker.app` / `Demo123!` (seed via `backend/scripts/seed-demo-data.sh`)

---

## Slide 1 — AI Job Tracker — Capstone

**Bullets:**
- Full-stack app for tracking job applications
- AI compares resume text to job descriptions and suggests improvements
- Capstone project — Dunwoody web programming degree

**Speaker notes (~45s):**  
Open with the problem: job seekers juggle many applications across spreadsheets or notes, with little feedback on whether a resume fits a role. AI Job Tracker centralizes applications, stores resumes, and uses OpenAI to analyze fit. This deck covers what we built and how we ship it safely with a Jenkins CI/CD pipeline on Linode.

---

## Slide 2 — What It Does

**Bullets:**
- Register and login with JWT authentication
- CRUD job applications with status workflow (Draft → Applied → Interviewing → Offer → Rejected)
- Upload resumes (PDF, DOCX, plain text)
- AI analysis: summary, strengths, weaknesses, suggestions, match score

**Speaker notes (~45s):**  
Walk through the user journey: sign up, add applications, upload a resume, link it to an application, run AI analysis. For a live demo, seed data first from `backend/` with `./scripts/seed-demo-data.sh`, then sign in as `demo@jobtracker.app` with password `Demo123!` — that account has sample applications and analyses ready to show.

---

## Slide 3 — Tech Stack

**Bullets:**
- **Frontend:** React 19, TypeScript, Vite 7, React Router, Bootstrap 5, Axios
- **Backend:** ASP.NET Core 8, EF Core 8, Npgsql, JWT, BCrypt
- **Data:** PostgreSQL 15 (Docker)
- **AI:** OpenAI Chat Completions (`gpt-4.1-mini`)
- **Ops:** Docker, Docker Compose, nginx, Linode (Ubuntu 24.04)

**Speaker notes (~40s):**  
The stack is a conventional modern SPA plus REST API. Resume text is extracted with PdfPig and OpenXml. Be transparent: the README lists Redis, but Redis is not wired into the app today, and `JobTracker.Worker` is a placeholder background service project — not part of production deploy.

---

## Slide 4 — Architecture

**Bullets:**
- **Production path:** Browser → nginx → static React (`/var/www/jobtracker`) + `/api` proxy → Dockerized .NET API → PostgreSQL
- **External:** OpenAI API for analysis; resume files on container disk (`/app/uploads`)
- **Backend layers:** API (controllers, DTOs) → Core (entities, services) → Infrastructure (EF, repositories)

**Speaker notes (~45s):**  
In production, nginx serves the Vite-built SPA and reverse-proxies `/api/` to the API container on localhost (default port 5001). The API and Postgres run under Docker Compose with a named volume for database persistence. Development uses Vite’s proxy to the API. This separation — static frontend, API in containers — is what the deploy stage publishes on every successful pipeline run.

**Diagram (production topology):**
```
Browser → nginx → React static (/var/www/jobtracker)
              └→ /api/ → .NET API (Docker) → PostgreSQL
                                    └→ OpenAI API
```

---

## Slide 5 — Security & API Design

**Bullets:**
- Passwords hashed with BCrypt; API protected with JWT Bearer tokens
- User-scoped data via `/me` endpoints and authorization checks
- DTOs for API contracts; pagination, search, and sort on list endpoints
- Secrets in `backend/.env` (Postgres, JWT, OpenAI, CORS) — not committed to git

**Speaker notes (~35s):**  
Security is layered: auth at the edge, user isolation in queries, and secrets only on the server. Swagger is enabled in Development only. This slide bridges to CI/CD: because we handle credentials and user data, the pipeline runs SAST, DAST, and container vulnerability scans before every deploy.

---

## Slide 6 — CI/CD Philosophy

**Bullets:**
- **Jenkins** (CI/CD server): runs our build pipeline automatically on each trigger
- Not using GitHub Actions — we own the full pipeline on a Linode server
- Pipeline defined in `backend/Jenkinsfile` — version-controlled like application code
- Every successful run deploys to production — quality and security checks run first

**Speaker notes (~50s):**  
Explain CI/CD in plain terms: Continuous Integration means every change is built and tested automatically; Continuous Delivery means a passing pipeline can go live without manual steps. We chose Jenkins on our own server for the capstone so we understand provisioning, agents, and credentials — not just clicking “Deploy” in a SaaS UI.

---

## Slide 7 — Infrastructure & Jenkins Agent

**Bullets:**
- **Linode:** cloud Linux server that hosts the live app and the Jenkins build agent
- **Jenkins agent:** the worker machine that executes pipeline steps (must have Docker)
- **Setup scripts:** one-time install of Docker, .NET 8, Node 20, and nginx on the server
- **GitHub:** stores our source code; Jenkins clones the `main` branch on every build

**Speaker notes (~50s):**  
Think of Jenkins as the conductor and the Linode agent as the musician — it runs builds, scans, and deploy on the same machine that serves users. Scripts in `deploy/` provision and bootstrap the server. The agent needs Docker to build images, spin up ZAP’s test stack, and run Trivy.

**Diagram:**
```
GitHub → Jenkins → Linode agent (build, scan, and deploy on same machine)
```

---

## Slide 8 — Pipeline Overview — 8 Stages

**Bullets:**
1. **Checkout** (Jenkins + GitHub) — download the latest source code
2. **SonarQube** — run unit tests; find code smells, bugs, and coverage gaps
3. **Snyk** — scan source code for security vulnerabilities (SAST)
4. **OWASP ZAP** — attack the running app for web flaws (DAST) — **blocks deploy**
5. **Docker Build** — package the .NET API into a container image
6. **Trivy** — scan the container image for known CVEs
7. **Build Frontend** — compile React into static files for nginx
8. **Deploy** — start containers, publish the website, reload nginx

**Speaker notes (~75s):**  
This is the main CI/CD slide. For each stage, name the tool and what problem it solves. Emphasize that stages 2–6 are third-party or industry-standard tools, not custom code. Stage 4 (ZAP) is our strictest gate. End with stage 8 making the app reachable to users.

---

## Slide 9 — SonarQube — Code Quality & Tests

**Bullets:**
- **SonarQube** (self-hosted, open source): code quality and maintainability dashboard
- Detects **code smells**, **bugs**, **duplicated code**, and overly complex methods
- Runs our **automated API tests** (xUnit) — login, users, resume parsing, etc.
- Measures **test coverage** — shows which parts of the codebase have no tests
- **Soft-fail today:** pipeline continues even if the quality gate fails

**Speaker notes (~60s):**  
SonarQube answers “Is this code healthy and tested?” not “Can a hacker break in?” — that’s the security tools on the next slide. Tests live in `JobTracker.Tests`. Results appear in Sonar’s web UI and JUnit reports in Jenkins. We intentionally soft-fail so a capstone demo isn’t blocked by a minor smell, but we still get the report.

---

## Slide 10 — Security Scanning — Three Tools

**Bullets:**
- **Snyk** (third-party SaaS): scans C# **source code** before deploy — unsafe patterns, hardcoded secrets, vulnerable dependencies in code
- **OWASP ZAP** (open source): crawls our **running API and UI** like a hacker — XSS, misconfigurations, exposed endpoints — **blocks deploy**
- **Trivy** (open source): scans the **Docker image** for known CVEs in packages and the OS — HIGH/CRITICAL severity
- **Defense in depth:** source code → running application → container image
- **Hard stops today:** ZAP failure, Docker build failure, frontend build failure, or deploy failure

**Speaker notes (~75s):**  
Use the “three layers” story: Snyk = static (reading code), ZAP = dynamic (hitting URLs), Trivy = shipping (what’s inside the image). ZAP runs against an isolated Docker stack so it never touches production user data. Mention that Snyk and Trivy are soft-fail — we archive reports and fix over time; ZAP must pass to ship.

**Blocking vs soft-fail:**

| Stage | Tool | What it checks | Blocks deploy? |
|-------|------|----------------|----------------|
| Quality | SonarQube | Smells, bugs, coverage, unit tests | No |
| SAST | Snyk | Vulnerabilities in source code | No |
| DAST | OWASP ZAP | Vulnerabilities in running app | **Yes** |
| Build | Docker | API image builds successfully | **Yes** |
| Container scan | Trivy | CVEs in image | No |
| Build | Vite/npm | Frontend compiles | **Yes** |
| Release | deploy.sh | App goes live | **Yes** |

---

## Slide 11 — Build Artifacts

**Bullets:**
- **Docker:** turns the .NET API into a portable **container image** (`jobtracker-api:latest`)
- Image runs as a **non-root user** — limits damage if the container is compromised
- **Vite:** compiles React + TypeScript into static HTML/JS/CSS in `frontend/dist/`
- **Docker Compose:** starts API + PostgreSQL together with **persistent database storage**
- **Note:** image is built twice today (in Jenkins and again at deploy) — room to optimize

**Speaker notes (~50s):**  
Artifacts are what we ship: one Docker image for the API, one folder of static files for the UI. Containers make “works on my machine” reproducible on Linode. The duplicate Docker build is a known simplification — ideal fix is build once, tag with build number, deploy that tag.

---

## Slide 12 — Deploy Stage

**Bullets:**
- **`deploy.sh`:** our script that publishes a new version to the Linode server
- Starts **API and PostgreSQL** containers; waits until the database and API are healthy
- Copies the **React build** to `/var/www/jobtracker` for nginx to serve
- **nginx:** reverse proxy — serves the website and forwards `/api` to the API container
- **Database volume is never deleted** — user data survives redeploys

**Speaker notes (~60s):**  
Deploy is the last pipeline stage and the only one users care about visibly. Walk through: containers up → health checks → static files copied → nginx reload. Schema is created when the API starts. If compose fails, we retry without wiping the database volume.

---

## Slide 13 — Secrets & Environments

**Bullets:**
- **Jenkins** stores integration passwords: GitHub access, Snyk API token, SonarQube token
- **Server `.env` file** stores app secrets: database password, JWT signing key, OpenAI key
- Secrets are **never committed to git** — only `.env.example` templates in the repo
- **Single environment today:** every pipeline run targets `main` and deploys to production

**Speaker notes (~45s):**  
Never show real secrets on slides. Two buckets: Jenkins credentials for tools, server `.env` for the running app. Future improvement: a staging server and branch so production deploy isn’t tied to every `main` commit.

---

## Slide 14 — Lessons, Gaps & Q&A

**Bullets:**
- **What we learned:** quality, security, and deploy as one automated pipeline
- **Gaps:** no frontend lint or tests in CI; no browser end-to-end tests; some scanners are soft-fail
- **Future:** GitHub Actions for pull requests; staging environment; stricter quality gates
- **Live demo:** `demo@jobtracker.app` / `Demo123!`

**Speaker notes (~45s):**  
Close with honesty — reviewers respect known gaps. Highlight what you learned: what each third-party tool does, how stages connect, and how code reaches users. Optional: show one AI analysis in the UI. Open for questions.

---

## Appendix — Key file references

| Topic | Path |
|-------|------|
| Pipeline | `backend/Jenkinsfile` |
| Deploy | `backend/scripts/deploy.sh` |
| Sonar + tests | `backend/scripts/run-sonar.sh` |
| ZAP | `backend/scripts/run-owasp-zap.sh` |
| Trivy | `backend/scripts/run-trivy.sh` |
| nginx | `deploy/nginx/jobtracker.conf` |
| Provision | `deploy/linode/provision.sh` |
| Seed demo | `backend/scripts/seed-demo-data.sh` |
