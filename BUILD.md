# BUILD — HowToSoftware .NET 10 Clone
## Ghost CMS → Docker / C# / T-SQL / .NET 10

> Replacing Ghost 6.19.2 (Node.js / MySQL) with a custom-built ASP.NET Core 10 platform  
> running on SQL Server 2025 in Docker — feature-matched to howtoosoftware.com

---

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / C# 14 |
| Web framework | ASP.NET Core 10 (Razor Pages + Web API) |
| Admin panel | Blazor Server (interactive) |
| Database | SQL Server 2025 (T-SQL) |
| ORM | Entity Framework Core 10 |
| Auth | ASP.NET Identity + magic-link tokens |
| Email | MailKit → Mailgun relay |
| Containers | Docker / Docker Compose |
| Reverse proxy | Caddy 2 |
| Rich-text editor | Lexical.js (JavaScript, embedded in Blazor) |
| Front-end build | Vite (CSS/JS bundling) |

---

### Phase 11 — Testing & Quality
| # | Task | Details | Status |
|---|---|---|---|
| 11.1 | Core.Tests | Unit tests: LexicalRenderer, MobiledocRenderer, SlugGenerator, ContentService, SettingsService | ☑ |
| 11.2 | Infrastructure.Tests | Integration tests: repositories against SQL Server (Testcontainers), email service mocks | ☑ |
| 11.3 | Web.Tests | Razor page tests, Content API endpoint tests, routing tests | ☐ |
| 11.4 | Admin.Tests | Blazor component tests (bUnit), editor integration tests | ☐ |
| 11.5 | Docker integration test | `docker compose up` → health checks pass → hit Content API → verify response | ☐ |
| 11.6 | Security audit | OWASP Top 10 review: XSS in rendered content, SQL injection (parameterized queries), CSRF tokens, auth bypass, rate limiting, secrets management | ☐ |
| 11.7 | Performance baseline | Load test homepage, post page, Content API — target <200ms p95 | ☐ |

---

### Phase 12 — Deployment & Ops
| # | Task | Details | Status |
|---|---|---|---|
| 12.1 | CI/CD pipeline | GitHub Actions: build → test → Docker build → push to registry | ☐ |
| 12.2 | Production docker-compose | Caddy auto-HTTPS, volume mounts, resource limits, logging | ☑ |
| 12.3 | Database backup | Scheduled SQL Server backup container (daily full, hourly diff) | ☐ |
| 12.4 | Health checks | `/health` endpoint (DB connectivity, disk space), Docker HEALTHCHECK | ☑ |
| 12.5 | Logging | Structured logging (Serilog) → stdout (Docker) + optional Seq/Application Insights | ☐ |
| 12.6 | DNS cutover | Point howtoosoftware.com → new stack, verify SSL, redirect old Ghost URLs | ☑ |

---

## Table Mapping Summary

Ghost CMS has **78 tables** in the `ghost` database and **17 tables** in the `activitypub` database (95 total).

### Ghost DB → T-SQL (78 tables, grouped)

**Content (9)**
posts, posts_meta, post_revisions, mobiledoc_revisions, tags, posts_tags, posts_authors, collections, collections_posts

**Users & Roles (5)**
users, roles, roles_users, permissions, permissions_roles, permissions_users

**Members (19)**
members, members_labels, labels, members_newsletters, members_products, members_stripe_customers, members_stripe_customers_subscriptions, members_cancel_events, members_click_events, members_created_events, members_email_change_events, members_feedback, members_login_events, members_paid_subscription_events, members_payment_events, members_product_events, members_status_events, members_subscribe_events, members_subscription_created_events

**Email (7)**
emails, email_batches, email_recipients, email_recipient_failures, email_spam_complaint_events, automated_emails, automated_email_recipients

**Commerce (8)**
products, products_benefits, benefits, stripe_products, stripe_prices, subscriptions, offers, offer_redemptions, donation_payment_events

**Comments (3)**
comments, comment_likes, comment_reports

**Analytics (1)**
tinybird_analytics_backup

**Settings & Config (3)**
settings, custom_theme_settings, snippets

**Infrastructure (9)**
actions, api_keys, integrations, invites, sessions, tokens, brute, jobs, migrations, migrations_lock

**Feeds & Discovery (5)**
webhooks, redirects, recommendations, recommendation_click_events, recommendation_subscribe_events, mentions, milestones, newsletters, outbox, suppressions

### ActivityPub DB → T-SQL (17 tables)
accounts, account_delivery_backoffs, blocks, domain_blocks, feeds, follows, ghost_ap_post_mappings, key_value, likes, mentions (AP), notifications, outboxes, posts (AP), reposts, schema_migrations, sites, users (AP)

---

## Reference Documents

| File | Description |
|---|---|
| [DOCS/OLLAMA_SETUP.md](DOCS/OLLAMA_SETUP.md) | Ollama + local LLM setup (Cursor and optional .NET app) |
| [DOCS/CLONE-ARCHITECTURE.md](DOCS/CLONE-ARCHITECTURE.md) | Solution structure, T-SQL schema, Docker Compose, implementation details |
| [DOCS/GHOST-DEPLOYMENT-DATA.md](DOCS/GHOST-DEPLOYMENT-DATA.md) | Complete extraction of the running Ghost instance (config, content, settings) |
| [DOCS/ghost_schema.sql](DOCS/ghost_schema.sql) | Raw MySQL schema dump (78 Ghost + 17 ActivityPub tables) |
| [DOCS/ghost_posts_dump.sql](DOCS/ghost_posts_dump.sql) | Content data export |
| [REF/howtoosoftware-custom/](REF/howtoosoftware-custom/) | Current Ghost theme (Handlebars templates, CSS, JS) to port |
| [DOCS/DNS-CUTOVER.md](DOCS/DNS-CUTOVER.md) | DNS cutover runbook — pre-checks, cutover steps, SSL verification, rollback plan |
