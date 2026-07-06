# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- _Nothing yet._

### Changed
- _Nothing yet._

### Deprecated
- _Nothing yet._

### Removed
- _Nothing yet._

### Fixed
- _Nothing yet._

### Security
- _Nothing yet._

## [0.1.0] - 2026-07-05

### Added

**Publishing & content**
- Posts, pages, tags, collections, and revisions with Ghost-style routing and pagination.
- Custom Lexical and legacy Mobiledoc renderers (JSON → sanitized HTML) plus a Mobiledoc→Lexical converter.
- Reusable snippets, slug generation, HTML sanitization, full-text search, and RSS/newsletter archive feeds.

**Members & commerce**
- ASP.NET Identity with passwordless magic-link sign-in.
- Stripe-backed paid subscriptions, tiers/products, offers, and one-off donations.
- Member labels, segments, notes, activity timeline, CSV import, staff impersonation, and tiered content gating (free / members / paid).

**Email & newsletters**
- Newsletter sending via MailKit → Mailgun with batched delivery.
- Automated emails, drip sequences, and A/B subject-line testing with holdout resolution.
- Suppression list and spam-complaint handling.

**Community & discovery**
- Threaded comments with likes and reports.
- ActivityPub federation (accounts, follows, likes, reposts, notifications, outbox).
- Webmentions/mentions, recommendations, redirects, IndexNow, and outbound webhooks.

**Analytics & operations**
- Privacy-friendly first-party analytics with hourly/daily rollups.
- Real-time live-visitor count and recent pageviews over SignalR.
- GeoIP country lookup, brute-force protection, admin audit log, and per-endpoint rate limiting.

**APIs & migration**
- Ghost-compatible Content API (REST) with API-key authentication.
- Standalone MySQL → SQL Server migration tool with rendered-HTML verification.

**Platform**
- Clean layered solution (Core / Infrastructure / Web / Admin / Migrator) on .NET 10 / C# 14 and EF Core 10.
- SQL Server 2025 schema porting Ghost and ActivityPub (95 tables).
- Docker Compose stack behind a TLS-terminating Caddy edge proxy with health checks.

### Security
- All rendered content passes through HTML sanitization; database access uses parameterized queries via EF Core.
- Per-IP and per-key sliding-window rate limiting, with staff/admin actions recorded in an audit log.
- Secrets are read from User Secrets and environment variables — never committed to `appsettings.json`.

[Unreleased]: <REPO_URL>/compare/v0.1.0...HEAD
[0.1.0]: <REPO_URL>/releases/tag/v0.1.0
