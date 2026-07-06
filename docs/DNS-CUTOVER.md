# DNS Cutover Runbook — howtoosoftware.com

Ghost CMS (Node.js / MySQL) → ASP.NET Core 10 (C# / SQL Server / Docker)

Architecture: Edge Caddy (<edge-proxy-ip>, TLS) → App Server (<app-server-ip>, web:80 + admin:5001)

---

## Prerequisites

- [x] All content migrated and verified (posts, pages, members, images)
- [x] `scripts/deploy.ps1` completed successfully on the target server
- [x] Health checks passing: `curl http://<server-ip>:80/health` returns `Healthy`
- [x] Admin panel accessible: `http://<server-ip>:5001/ghost/`
- [ ] Content API responding: `curl http://<server-ip>:80/api/content/posts/?key=<api-key>`
- [ ] RSS feed valid: `http://<server-ip>:80/rss/`
- [ ] Sitemap valid: `http://<server-ip>:80/sitemap.xml`
- [ ] `.env` file on server has `SITE_URL=howtoosoftware.com`
- [ ] Mailgun DNS records configured for `mg.howtoosoftware.com`
- [ ] Database backup taken and stored off-server

ssh <user>@<app-server-ip> "echo 'MAILGUN_API_KEY=your-key-here' >> ~/howtoosoftware/.env"

---

## Pre-Cutover Verification (direct to app server)

```bash
SERVER=<app-server-ip>

# Health checks
curl -s http://$SERVER:80/health
# Expected: Healthy

# Homepage loads
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/
# Expected: 200

# Post page loads (use a known slug)
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/welcome/
# Expected: 200

# Tag page loads
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/tag/news/
# Expected: 200

# Author page loads
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/author/henry/
# Expected: 200

# RSS feed
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/rss/
# Expected: 200

# Sitemap
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/sitemap.xml
# Expected: 200

# Ghost API redirect (versioned) — handled by edge Caddy on <edge-proxy-ip>
curl -s -o /dev/null -w "%{http_code} → %{redirect_url}" https://howtoosoftware.com/ghost/api/v4/content/posts/?key=test
# Expected: 301 → /api/content/posts/?key=test

# Trailing-slash enforcement
curl -s -o /dev/null -w "%{http_code} → %{redirect_url}" http://$SERVER:80/welcome
# Expected: 301 → /welcome/

# Admin panel (direct)
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:5001/ghost/
# Expected: 200

# Admin panel (via edge proxy)
curl -s -o /dev/null -w "%{http_code}" https://howtoosoftware.com/ghost/
# Expected: 200

# Member auth pages
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/signin/
# Expected: 200
curl -s -o /dev/null -w "%{http_code}" http://$SERVER:80/signup/
# Expected: 200
```

---

## DNS Cutover Steps

### 1. Lower TTL (24 hours before cutover)

At your DNS provider, lower the A record TTL for `howtoosoftware.com` to **60 seconds**.
This ensures fast propagation when you update the record.

```
howtoosoftware.com.  60  IN  A  <current-ghost-server-ip>
```

Wait at least the previous TTL duration (typically 1–24 hours) for the low TTL to propagate.

### 2. Shut down Ghost (optional — zero-downtime alternative below)

If you want to prevent stale content being served during propagation:
```bash
# On old Ghost server
docker compose down
```

Alternatively, leave Ghost running during propagation for zero downtime.

### 3. Update DNS records

At your DNS provider, update the records:

```
# A record — point to new server
howtoosoftware.com.     60  IN  A  <new-server-ip>

# www CNAME — point to apex
www.howtoosoftware.com. 60  IN  CNAME  howtoosoftware.com.
```

If using IPv6:
```
howtoosoftware.com.     60  IN  AAAA  <new-server-ipv6>
```

### 4. Restart app containers

On the app server (<app-server-ip>), rebuild and restart the containers:

```bash
cd ~/howtoosoftware
docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build --remove-orphans
```

### 5. Verify SSL certificate

The edge Caddy proxy (<edge-proxy-ip>) handles TLS via Let's Encrypt.
DNS already points to <edge-proxy-ip> — verify the certificate is valid:

```bash
# Check certificate from outside the server
curl -vI https://howtoosoftware.com 2>&1 | grep -E "SSL certificate|subject:|expire"

# Verify HSTS header
curl -sI https://howtoosoftware.com | grep -i strict-transport

# Verify www redirect
curl -sI https://www.howtoosoftware.com | grep -i location
# Expected: location: https://howtoosoftware.com/
```

### 6. Post-cutover verification

```bash
# All routes over HTTPS
curl -s -o /dev/null -w "%{http_code}" https://howtoosoftware.com/
curl -s -o /dev/null -w "%{http_code}" https://howtoosoftware.com/health
curl -s -o /dev/null -w "%{http_code}" https://howtoosoftware.com/rss/
curl -s -o /dev/null -w "%{http_code}" https://howtoosoftware.com/sitemap.xml
curl -s -o /dev/null -w "%{http_code}" https://howtoosoftware.com/ghost/

# Security headers
curl -sI https://howtoosoftware.com | grep -iE "x-content-type|x-frame|strict-transport|content-security|referrer"

# Ghost API redirect works over HTTPS
curl -sI https://howtoosoftware.com/ghost/api/v4/content/posts/?key=test | grep location

# Monitor container logs
docker compose logs -f --tail=50
```

### 7. Restore TTL

After confirming everything works (wait at least 1 hour), restore the DNS TTL:

```
howtoosoftware.com.  3600  IN  A  <new-server-ip>
```

---

## Rollback Plan

If critical issues are found after cutover:

1. **Immediate**: Point DNS back to the old Ghost server IP
   ```
   howtoosoftware.com.  60  IN  A  <old-ghost-server-ip>
   ```

2. **If Ghost was shut down**: Restart Ghost containers on the old server
   ```bash
   cd ~/ghost && docker compose up -d
   ```

3. **Max rollback window**: 60 seconds (TTL was lowered in step 1)

---

## Post-Cutover Monitoring (48 hours)

- [ ] Check `/health` endpoint every hour for first 4 hours
- [ ] Review container logs for 4xx/5xx errors: `docker compose logs web | grep -E "HTTP/[12].[01]\" [45]"`
- [ ] Verify RSS feed validated at https://validator.w3.org/feed/
- [ ] Verify sitemap submitted to Google Search Console
- [ ] Confirm Mailgun email delivery working (send test magic link)
- [ ] Check analytics events recording to database
- [ ] Monitor disk space (images volume)
- [ ] Verify IndexNow pings reaching search engines

---

## Post-Cutover Cleanup (after 7 days stable)

- [ ] Raise DNS TTL to 3600 (1 hour)
- [ ] Archive Ghost MySQL database dump
- [ ] Decommission Ghost containers and server
- [ ] Remove Ghost Docker images
- [ ] Update Google Search Console sitemap URL
- [ ] Submit updated sitemap to Bing Webmaster Tools
- [ ] Notify IndexNow of all page URLs for re-indexing

---

## URL Mapping Reference

| Ghost URL Pattern | .NET URL | Status |
|---|---|---|
| `/{slug}/` (post) | `/{slug}/` | Identical |
| `/{slug}/` (page) | `/{slug}/` | Identical |
| `/tag/{slug}/` | `/tag/{slug}/` | Identical |
| `/author/{slug}/` | `/author/{slug}/` | Identical |
| `/page/{n}/` | `/page/{n}/` | Identical |
| `/rss/` | `/rss/` | Identical |
| `/content/images/*` | `/content/images/*` | Identical |
| `/ghost/` (admin) | `/ghost/` → Blazor admin | Edge Caddy routes to <app-server-ip>:5001 |
| `/ghost/api/v{n}/content/*` | `/api/content/*` | 301 redirect (edge Caddy + middleware) |
| `/ghost/api/content/*` | `/api/content/*` | 301 redirect (edge Caddy + middleware) |
| `/ghost/sitemap.xml` | `/sitemap.xml` | 301 redirect |
| `/sitemap-posts.xml` | `/sitemap.xml` | 301 redirect |
| `/sitemap-pages.xml` | `/sitemap.xml` | 301 redirect |
| `/sitemap-tags.xml` | `/sitemap.xml` | 301 redirect |
| `/sitemap-authors.xml` | `/sitemap.xml` | 301 redirect |
| `/#/portal/signin` | `/signin/` | Hash route (client-side only) |
| `/#/portal/signup` | `/signup/` | Hash route (client-side only) |
| `/#/portal/account` | `/account/` | Hash route (client-side only) |
| Custom redirects | DB-driven | Via Redirects table in admin |
