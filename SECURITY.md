# Security Policy

## Supported Versions

| Version | Supported          |
| ------- | ------------------ |
| 1.x     | ✅ Yes              |
| < 1.0   | ❌ No               |

## Reporting a Vulnerability

We take security issues seriously. Please **do not** file a public GitHub issue for
security vulnerabilities.

### Preferred method — GitHub private vulnerability reporting

Use [GitHub's private vulnerability reporting](https://github.com/sassdawe/browseraptor/security/advisories/new)
to open a draft security advisory. This keeps the disclosure private until a fix is available.

### Alternative method — email

If you are unable to use the GitHub advisory form, send details to the maintainer via email
(address available on the GitHub profile for [@sassdawe](https://github.com/sassdawe)).

### What to include

Please provide as much of the following as possible:

- A clear description of the vulnerability and its potential impact.
- Steps to reproduce the issue (proof-of-concept or reproduction script if available).
- The affected version(s) of BrowserAptor.
- Any suggested mitigations or patches you have already identified.

## Response SLA

| Severity | Initial response | Fix target |
| -------- | ---------------- | ---------- |
| Critical | 48 hours         | 7 days     |
| High     | 72 hours         | 14 days    |
| Medium   | 7 days           | 30 days    |
| Low      | 14 days          | Next minor release |

We will confirm receipt of your report and keep you updated as we work through the fix.

## Disclosure policy

We follow responsible disclosure:

1. The reporter submits the issue privately.
2. We acknowledge and investigate.
3. We develop and test a fix.
4. We publish the fix and create a GitHub Security Advisory with CVE attribution where appropriate.
5. We credit the reporter in the advisory (unless they prefer to remain anonymous).

## Security tooling

This repository uses the following automated security tooling:

- **CodeQL** — static analysis on every push to `main` and every pull request.
- **Dependency Review** — checks NuGet dependency changes on every pull request for known vulnerabilities.
- **NuGet vulnerability audit** — `dotnet list package --vulnerable` runs in CI on every build.
- **Dependabot** — automatically opens PRs for NuGet and GitHub Actions updates.
- **SBOM** — a CycloneDX JSON Software Bill of Materials is generated and published with every release.
- **Artifact attestation** — release artifacts are signed with GitHub's build provenance attestation.
