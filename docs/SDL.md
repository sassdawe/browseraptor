# Secure Development Lifecycle (SDL) Policy

This document describes the security controls, gates, and processes that govern every
change merged into BrowserAptor and every artifact published to users.

---

## Table of Contents

1. [Scope and ownership](#1-scope-and-ownership)
2. [PR gates — required checks](#2-pr-gates--required-checks)
3. [Severity thresholds](#3-severity-thresholds)
4. [Release pipeline](#4-release-pipeline)
5. [Software Bill of Materials (SBOM)](#5-software-bill-of-materials-sbom)
6. [Artifact attestation](#6-artifact-attestation)
7. [Dependency management](#7-dependency-management)
8. [Triage SLA](#8-triage-sla)
9. [Rollout phases](#9-rollout-phases)
10. [Runbook — failed security checks](#10-runbook--failed-security-checks)
11. [Runbook — emergency release rollback](#11-runbook--emergency-release-rollback)

---

## 1. Scope and ownership

| Area | Owner |
|------|-------|
| Security policy | Repository maintainer (@sassdawe) |
| Vulnerability disclosure | See [SECURITY.md](../SECURITY.md) |
| CI / workflow maintenance | Repository maintainer |
| Dependency updates | Dependabot (automated PRs, reviewed by maintainer) |

---

## 2. PR gates — required checks

The following checks **must pass** before a pull request can be merged into `main`.

| Check | Workflow | Failure action |
|-------|----------|----------------|
| Build & test (Ubuntu) | `ci.yml` | Block merge |
| NuGet vulnerability audit | `ci.yml` | Block merge |
| CodeQL static analysis | `codeql.yml` | Block merge |
| Dependency review | `dependency-review.yml` | Block merge (High+) |
| Pre-release Windows build | `pre-release.yml` | Block merge |

All of these are enforced as [required status checks](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches#require-status-checks-before-merging)
on the `main` branch. Configure them in **Settings → Branches → Branch protection rules → main**.

---

## 3. Severity thresholds

### NuGet vulnerability audit

`dotnet list package --vulnerable --include-transitive` is run in both `ci.yml` and
`release.yml`. The command exits non-zero for **any** severity level, blocking the
build immediately.

### Dependency review

The `dependency-review.yml` workflow blocks a PR when a dependency change introduces a
vulnerability with severity **High or Critical** (configured via `fail-on-severity: high`).
Medium and Low findings are surfaced as informational comments.

### CodeQL

All findings from the `security-extended` query suite are surfaced in the Security tab.
Findings at severity **High or Critical** must be triaged before the associated PR is
merged. Findings at lower severities may be accepted with a written justification in the
security advisory.

---

## 4. Release pipeline

Every push to `main` (i.e., every merged PR) triggers the `release.yml` workflow, which
publishes a **pre-release** GitHub Release. When the maintainer is ready to cut an
official versioned release, they push a semver tag (e.g., `git tag v1.2.0 && git push --tags`),
which triggers the same workflow and creates a **full release**.

### Pipeline steps (in order)

```
checkout
  └─> restore NuGet packages
        └─> NuGet vulnerability audit          ← security gate; aborts on any finding
              └─> build (Release config)
                    └─> run tests
                          └─> compute release version
                                └─> publish win-x64 self-contained executable
                                      └─> zip artifact
                                            └─> generate SBOM (CycloneDX JSON)
                                                  └─> verify SBOM file exists
                                                        └─> attest build provenance
                                                              └─> create GitHub Release
                                                                    └─> upload workflow artifacts
```

### Versioning

| Trigger | Version format | Release type |
|---------|----------------|--------------|
| Tag push `v*` | Tag name (e.g. `v1.2.0`) | Full release |
| Push to `main` | `v{csproj_version}-dev.{yyyyMMdd}.{sha}` | Pre-release |

The `<Version>` in `src/BrowserAptor/BrowserAptor.csproj` is the authoritative source of
the base version number for dev releases.

### Permissions

The release job uses the minimum set of GitHub token permissions required:

| Permission | Reason |
|------------|--------|
| `contents: write` | Create GitHub releases and upload release assets |
| `id-token: write` | Obtain the OIDC token used for artifact attestation |
| `attestations: write` | Write build-provenance attestations to the repository |

---

## 5. Software Bill of Materials (SBOM)

An SBOM is generated for every release using the
[CycloneDX .NET global tool](https://github.com/CycloneDX/cyclonedx-dotnet) (v4.1.0).

### Format

CycloneDX v1 JSON (`bom.json`), published as `browseraptor-sbom.json` attached to the
GitHub Release.

### Generation

The SBOM is generated from the release project's MSBuild dependency graph:

```
dotnet CycloneDX src/BrowserAptor/BrowserAptor.csproj -o sbom -fn bom -j
```

This captures all direct and transitive NuGet package references resolved for the
`win-x64` release target.

### Where to find it

Every GitHub Release includes:
- `BrowserAptor-win-x64.zip` — the executable
- `browseraptor-sbom.json` — the SBOM

### How consumers can use the SBOM

1. **Vulnerability scanning** — ingest `browseraptor-sbom.json` into a tool such as
   [Grype](https://github.com/anchore/grype) or [OWASP Dependency-Check](https://owasp.org/www-project-dependency-check/)
   to check for known CVEs in BrowserAptor's dependencies.

2. **License compliance** — the SBOM lists the declared license for every NuGet package,
   enabling automated license policy checks.

3. **Audit trail** — the SBOM version, serial number, and timestamp provide a point-in-time
   snapshot of the dependency tree for a given release.

```bash
# Example: scan the SBOM with Grype
grype sbom:browseraptor-sbom.json
```

---

## 6. Artifact attestation

Both `BrowserAptor-win-x64.zip` and `browseraptor-sbom.json` are attested using
[GitHub's artifact attestation](https://docs.github.com/en/actions/security-guides/using-artifact-attestations-to-establish-provenance-for-builds)
via `actions/attest-build-provenance`. The attestation links each artifact to the exact
source commit and workflow run that produced it.

### Verifying attestations

```bash
# Install GitHub CLI if not already installed
# https://cli.github.com/

gh attestation verify BrowserAptor-win-x64.zip  --repo sassdawe/browseraptor
gh attestation verify browseraptor-sbom.json     --repo sassdawe/browseraptor
```

A successful verification confirms the artifact was built by the official workflow on
GitHub-hosted infrastructure.

---

## 7. Dependency management

### Automated updates — Dependabot

Dependabot is configured (`.github/dependabot.yml`) to open weekly PRs for:

- **NuGet packages** — all projects in the repository root
- **GitHub Actions** — all workflow files

Dependabot PRs are subject to the same PR gates as human contributions (CodeQL,
dependency review, vulnerability audit).

### Manual updates

When a vulnerability is disclosed in a dependency outside of the Dependabot schedule:

1. Check whether the vulnerability affects BrowserAptor's use of the package.
2. If affected: create a branch, update the package version, open a PR.
3. Reference the CVE or GitHub Advisory in the PR description.
4. Merge after all PR gates pass.

---

## 8. Triage SLA

| Severity | Initial response | Fix target |
|----------|------------------|------------|
| Critical | 48 hours | 7 days |
| High | 72 hours | 14 days |
| Medium | 7 days | 30 days |
| Low | 14 days | Next minor release |

Triage is tracked via GitHub Security Advisories. New findings from CodeQL, Dependabot,
or private reports are assigned a severity on receipt and tracked to resolution.

---

## 9. Rollout phases

### Phase 1 — Soft-fail (initial burn-in)

All new SDL workflows run and report results, but do not yet block merges. This allows
the team to observe the volume and nature of findings before enforcing gates.

**Duration:** Until the finding backlog is reviewed and triaged (target: first sprint
after SDL introduction).

> To enable soft-fail, add `continue-on-error: true` to the relevant workflow steps.
> Remove this when graduating to Phase 2.

### Phase 2 — Blocking on High/Critical

- CodeQL High/Critical findings block PRs.
- Dependency Review blocks PRs on High/Critical dependency changes.
- NuGet vulnerability audit blocks builds on any finding.
- Branch protection rules are updated to require all SDL status checks.

### Phase 3 — Full enforcement with environment protection

- Add a `production` GitHub Environment for the release workflow.
- Require a manual approval step before the release is published (optional).
- All SDL checks are required status checks on `main`.
- SBOM and attestation verification are validated before deployment wherever BrowserAptor is distributed.

---

## 10. Runbook — failed security checks

### CodeQL finding blocks PR

1. Navigate to **Security → Code scanning alerts** and read the full finding.
2. If it is a **true positive**:
   - Fix the code in the same PR or open a follow-up security PR.
   - Re-run CodeQL after the fix.
3. If it is a **false positive**:
   - Dismiss the alert in the Security tab with a written justification.
   - The PR can proceed after dismissal.
4. If the severity is below the blocking threshold (Medium / Low) but you want to track it:
   - Leave it open in the Security tab and continue the PR.

### Dependency review blocks PR

1. Read the `dependency-review-action` comment on the PR.
2. Identify the vulnerable package and the severity.
3. Options:
   - **Update the package** to a non-vulnerable version — preferred.
   - **Remove the dependency** if it is not needed.
   - **Accept the risk** (Low/Medium only) — document in the PR and add a `security-accepted-risk` label.
4. Re-push the branch to re-run dependency review.

### NuGet vulnerability audit fails

1. Run `dotnet list BrowserAptor.slnx package --vulnerable --include-transitive` locally.
2. Identify the affected package(s) and the advisory URL.
3. Update the package in the relevant `.csproj` file to a patched version.
4. Commit and push to re-run CI.

### SBOM generation fails in release workflow

1. Check the workflow logs for errors from the `Generate SBOM` step.
2. Common causes:
   - CycloneDX tool install failed (network issue) — retry the workflow.
   - Project file path changed — update the path in `release.yml`.
   - .NET restore failed — fix restore errors first.
3. Do **not** skip the SBOM step or remove the verification gate.

---

## 11. Runbook — emergency release rollback

If a published release is found to contain a critical defect or security vulnerability:

### Step 1 — Delete or retract the GitHub Release

```bash
# Mark the release as a draft to hide it from the releases page
gh release edit <tag> --draft --repo sassdawe/browseraptor

# Or delete the release entirely (does not delete the tag)
gh release delete <tag> --repo sassdawe/browseraptor
```

### Step 2 — Communicate (if a security issue)

- Open a GitHub Security Advisory immediately to track the issue privately.
- Do not disclose details publicly until a fix is available.

### Step 3 — Prepare a fix

- Create a branch from the affected tag: `git checkout -b fix/security-rollback <tag>`.
- Apply the fix, add tests, open a PR.
- All PR gates must pass before merging.

### Step 4 — Cut a patched release

- After merging the fix to `main`, push a new patch tag:
  ```bash
  git tag v{major}.{minor}.{patch+1}
  git push --tags
  ```
- The `release.yml` workflow runs automatically and publishes the patched release.

### Step 5 — Publish a security advisory

- Update the GitHub Security Advisory with the CVE, affected versions, and fixed version.
- Reference the advisory in the release notes of the patched release.
