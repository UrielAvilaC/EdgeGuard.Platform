# Contributing to EdgeGuard Platform

This document describes the development workflow, branching model, commit conventions, and code review process for EdgeGuard Platform contributors.

---

## Table of Contents

1. [Branching Model](#branching-model)
2. [Commit Conventions](#commit-conventions)
3. [Local Development Setup](#local-development-setup)
4. [Pull Request Process](#pull-request-process)
5. [PR Checklist](#pr-checklist)
6. [Code Review Process](#code-review-process)

---

## Branching Model

EdgeGuard uses a trunk-based branching strategy with short-lived feature branches.

### Branch types

| Branch | Purpose | Merged into | Protected |
|--------|---------|-------------|-----------|
| `main` | Production releases | — | Yes |
| `Development` | Integration branch; CI runs here | `main` | Yes |
| `feat/*` | New features | `Development` | No |
| `fix/*` | Bug fixes | `Development` | No |
| `chore/*` | Build/tooling/dependency updates | `Development` | No |
| `docs/*` | Documentation only changes | `Development` or `main` | No |
| `release/*` | Release preparation | `main` and `Development` | Yes |

### Rules

- **Never commit directly to `main` or `Development`** — all changes go through a pull request.
- Feature branches are short-lived: aim for less than 3 days of divergence from `Development`.
- Delete feature branches after merging.
- `main` always reflects production state. Any commit on `main` must be deployable.

### Branch naming examples

```
feat/hl7-oru-image-links
feat/node-telemetry-dashboard
fix/dicom-ae-title-case-sensitive
fix/study-stuck-sending-retry
chore/upgrade-fo-dicom-5.1
docs/pacs-integration-guide
```

---

## Commit Conventions

EdgeGuard uses [Conventional Commits](https://www.conventionalcommits.org/) format.

### Format

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

### Types

| Type | When to use |
|------|------------|
| `feat` | A new feature |
| `fix` | A bug fix |
| `docs` | Documentation changes only |
| `refactor` | Code restructuring, no behavior change |
| `test` | Adding or updating tests |
| `chore` | Build, tooling, dependency updates |
| `perf` | Performance improvement |
| `ci` | CI/CD pipeline changes |

### Scopes (optional but recommended)

| Scope | Area |
|-------|------|
| `hub` | Hub API or domain |
| `node` | Edge Node service |
| `spa` | Angular frontend |
| `hl7` | HL7 integration |
| `dicom` | DICOM services |
| `pacs` | PACS integration |
| `db` | Database / migrations |
| `auth` | Authentication / JWT |
| `routing` | DICOM routing rules |

### Examples

```
feat(hl7): add ORU^R01 OBX image link processing

Implements parsing of OBX segments in ORU^R01 messages to extract
image viewer URLs (RP value type) and attach them to the corresponding study.

Closes #142
```

```
fix(dicom): resolve AE Title case sensitivity in association validation
```

```
refactor(hub): extract HL7 message validation into dedicated service

Moves inline validation logic from Hl7MessageHandler into
Hl7MessageValidationService to improve testability.
```

```
chore(db): add migration AddHl7MrgObxSupport
```

---

## Local Development Setup

### Prerequisites

| Tool | Version | Install |
|------|---------|---------|
| .NET SDK | 10.0 | https://dotnet.microsoft.com/download |
| Node.js | 20 LTS | https://nodejs.org |
| Angular CLI | 17+ | `npm install -g @angular/cli` |
| Docker Desktop | Latest | https://www.docker.com/products/docker-desktop |
| EF Core Tools | Latest | `dotnet tool install --global dotnet-ef` |
| PostgreSQL | 16 | Via Docker (recommended) |

### Clone and configure

```bash
# Clone
git clone https://github.com/your-org/edgeguard-platform.git
cd edgeguard-platform

# Create local environment file (do not commit this)
cp .env.example .env.local
```

Edit `.env.local`:
```env
EDGEGUARD_HUB_CONNECTIONSTRING=Host=localhost;Port=5432;Database=edgeguard_dev;Username=edgeguard;Password=devpassword
Jwt__SecretKey=dev_secret_key_at_least_32_chars_long
ASPNETCORE_ENVIRONMENT=Development
```

### Start the database

```bash
docker run --name edgeguard-postgres \
  -e POSTGRES_USER=edgeguard \
  -e POSTGRES_PASSWORD=devpassword \
  -e POSTGRES_DB=edgeguard_dev \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### Build and run

```bash
# Restore all packages
dotnet restore

# Build all projects
dotnet build

# Run the Hub (from repo root)
dotnet run --project src/backend/Dicom.Edge.Hub.Api

# In a separate terminal — run the Edge Node
dotnet run --project src/backend/Dicom.Edge.Node

# In a separate terminal — run the Angular SPA (dev server)
cd src/frontend/dicomedge-ui
npm install
ng serve
```

The Hub runs on `http://localhost:5000` and the SPA dev server on `http://localhost:4200` (proxied to Hub API).

> **Local dev runs Kestrel directly** (no IIS). Production deploys to IIS — see [deployment-hub.md](../07-operations/deployment-hub.md). Don't develop against IIS Express; it has subtly different SignalR behaviour than the production AspNetCoreModuleV2 in-process host. To smoke-test the IIS path before merging, publish locally (`dotnet publish -c Release`) and point a temporary IIS site at the output.

### Run tests

```bash
# All unit tests
dotnet test --filter "Category=Unit"

# All integration tests (requires Docker for TestContainers)
dotnet test --filter "Category=Integration"

# All tests
dotnet test
```

---

## Pull Request Process

1. **Create a branch** from `Development`:
   ```bash
   git checkout Development
   git pull origin Development
   git checkout -b feat/your-feature-name
   ```

2. **Make changes** with small, focused commits following the commit convention.

3. **Run tests locally** before opening a PR:
   ```bash
   dotnet test
   cd src/frontend/dicomedge-ui && ng lint && ng test --watch=false
   ```

4. **Push and open a PR** targeting `Development`:
   ```bash
   git push origin feat/your-feature-name
   # Then open PR via GitHub/GitLab UI
   ```

5. **Fill in the PR description** using the PR template. Link to the related issue.

6. **Address review feedback** — push additional commits (do not force-push after review starts).

7. **Merge** — use **Squash and Merge** for feature branches to keep `Development` history clean.

---

## PR Checklist

Before requesting review, confirm all items:

**Code quality**
- [ ] No new compiler warnings introduced
- [ ] No TODO/FIXME left without a linked issue
- [ ] Nullable reference types enabled and warnings resolved
- [ ] No hardcoded secrets or connection strings

**Tests**
- [ ] New code has unit tests
- [ ] All existing tests pass (`dotnet test`)
- [ ] Integration tests pass if persistence layer changed
- [ ] No tests skipped without explanation

**Database**
- [ ] EF Core migration included if any entity or `DbContext` was modified
- [ ] Migration reviewed (Up/Down both correct)

**Frontend (if applicable)**
- [ ] `ng lint` passes with no warnings
- [ ] Component uses OnPush change detection
- [ ] No direct DOM manipulation
- [ ] Signals used for state (not RxJS BehaviorSubject for new code)

**Documentation**
- [ ] Public API methods have XML doc comments
- [ ] Relevant docs updated (if user-facing behavior changed)
- [ ] CHANGELOG entry added (for features and bug fixes)

---

## Code Review Process

### For reviewers

- **At least 1 approval required** before merge.
- Review within 1 business day for normal PRs; same day for critical `fix/*` PRs.
- Focus on correctness, security, and domain model integrity — style issues are handled by the linter.
- Use **Request Changes** only for blocking issues. Use **Comment** for suggestions.
- Approve once all blocking issues are resolved.

### For authors

- Respond to all comments before merging.
- If you disagree with feedback, discuss in the PR thread before overriding.
- Don't merge your own PR without at least 1 other approval (except emergency hotfixes with team lead sign-off).
- Don't rebase or force-push after review has started — it discards comment context.

### Review focus areas

| Area | What to check |
|------|--------------|
| Domain model | Invariants enforced? Aggregates modified only via domain methods? |
| Error handling | Result pattern used in application layer? No swallowed exceptions? |
| Security | PHI never logged? JWT validated? Input validated? |
| Migrations | Safe for production? No data-destructive defaults? |
| Performance | N+1 queries? Unbounded list queries? |
| Tests | New behavior covered? Edge cases included? |
