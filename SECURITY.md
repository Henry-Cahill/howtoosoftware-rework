# Security Policy

**HowToSoftware** is a self-hosted, ASP.NET Core 10 publishing platform that
handles member accounts, paid subscriptions, and email delivery. Because it
processes personal data and payment-related flows, we take security reports
seriously and appreciate the community's help in keeping the platform and its
users safe.

## Supported Versions

The project is in active pre-1.0 development. Security fixes are applied to the
latest release line only; there are no long-term-support branches yet. Once a
`1.0` release is cut, this table will be updated to reflect the supported
stable versions.

| Version | Supported          | Notes                                    |
| ------- | ------------------ | ---------------------------------------- |
| 0.1.x   | :white_check_mark: | Current development line — fixes land here |
| < 0.1   | :x:                | Unreleased / pre-history, not supported  |

Always run the most recent `0.1.x` build to receive security updates.

## Reporting a Vulnerability

**Please do not report security vulnerabilities through public GitHub issues,
pull requests, or discussions.** Public disclosure before a fix is available
puts every deployment at risk.

Instead, report privately using either of these channels:

- **Email:** [henry.cahill@howtoosoftware.com](mailto:henry.cahill@howtoosoftware.com)
- **GitHub:** [private vulnerability reporting](https://docs.github.com/en/code-security/security-advisories/guidance-on-reporting-and-writing-information-about-vulnerabilities/privately-reporting-a-security-vulnerability)
  on this repository (Security → Report a vulnerability)

If you would like to encrypt your report, ask us for a PGP key in an initial
(non-sensitive) message and we will provide one.

Please include as much of the following as you can:

- A description of the vulnerability and its potential impact
- The affected component (Web, Admin, Content API, Migrator, or infrastructure)
- Steps to reproduce or a proof of concept
- Affected version(s), configuration, and environment details
- Any logs, screenshots, or request/response captures that help us reproduce it
- Any suggested mitigation or fix

Please do **not** include the personal data of real members in your report.
Redact or use test accounts wherever possible.

## What to Expect

- **Acknowledgement:** we will confirm receipt of your report within
  **3 business days**.
- **Assessment & triage:** we will validate the issue, determine its severity
  (using CVSS as a guide), and keep you updated on our progress.
- **Fix & disclosure:** we will develop and test a fix and coordinate a
  disclosure timeline with you. We aim to resolve **critical** issues within
  **30 days** of triage, and lower-severity issues on a best-effort basis.
- **Credit:** with your permission, we are happy to credit you in the release
  notes or an advisory once the fix has shipped.

We follow a **coordinated disclosure** model: please give us a reasonable
opportunity to release a fix (typically up to **90 days**) before disclosing
details publicly.

## Safe Harbor

We consider security research and vulnerability disclosure conducted in good
faith under this policy to be authorized. We will not pursue or support legal
action against you for such research, provided you:

- Make a good-faith effort to avoid privacy violations, data destruction, and
  service interruption or degradation
- Only interact with accounts you own or have explicit permission to test
- Do not exfiltrate, retain, or share more data than is necessary to
  demonstrate the vulnerability, and securely delete any such data afterward
- Give us a reasonable time to remediate before any public disclosure

If in doubt about whether an action is authorized, ask us first.

## Scope

### In scope

- The application source in this repository: **HowToSoftware.Web**
  (public site + Content API), **HowToSoftware.Admin** (Blazor Server admin),
  **HowToSoftware.Core**, **HowToSoftware.Infrastructure**, and
  **HowToSoftware.Migrator**
- Authentication and session handling (ASP.NET Identity, magic-link tokens,
  API-key authentication, staff impersonation)
- Member, subscription, and payment-related flows (Stripe integration surfaces)
- Content rendering and sanitization (Lexical/Mobiledoc → HTML), file/image
  handling, and the import/export pipeline
- Access control and data-exposure issues (IDOR, tenant/tier gating bypass,
  privilege escalation)
- Infrastructure-as-code shipped in this repo (Dockerfiles, `docker-compose`,
  and the Caddy edge configuration)

### Out of scope

- Third-party services and their infrastructure (Stripe, Mailgun, Cloudflare,
  MaxMind/DB-IP) — report those to the respective vendor
- The reference Ghost theme and dumps under `ref/` and `docs/` (third-party or
  historical data, not part of the running application)
- Vulnerabilities in dependencies that are already publicly known — instead,
  let us know which package/version so we can upgrade
- Findings from automated scanners without a demonstrated, exploitable impact
- Denial-of-service, volumetric, or brute-force load testing against live
  systems
- Social engineering, phishing, or physical attacks against staff or infrastructure
- Missing security headers, cookie flags, or best-practice suggestions with no
  concrete exploit
- Self-XSS, and clickjacking on pages with no sensitive state-changing actions

## Security Measures

The platform ships with defense-in-depth controls, including magic-link
(passwordless) sign-in, bcrypt → PBKDF2 password migration, HTML sanitization
of user-generated content, brute-force protection, per-endpoint rate limiting,
an admin audit log, and a TLS-terminating Caddy edge proxy that keeps
databases and management interfaces off the public internet. Reports that
help us strengthen these controls are especially welcome.

---

Thank you for helping keep **HowToSoftware** and its users safe.

> **Maintainer note:** confirm that `henry.cahill@howtoosoftware.com` is a
> monitored inbox (or replace it with the correct address) before publishing
> this policy.
