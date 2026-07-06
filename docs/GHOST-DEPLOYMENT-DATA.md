# Ghost CMS Deployment — Complete Data Extraction
## howtoosoftware.com | Server: <SERVER_IP>

> Secrets and personal data in this document have been redacted (`<REDACTED>` /
> `<redacted>`). Restore real values from a secrets manager if you need them.

---

## 1. Server Infrastructure

| Component | Version / Detail |
|---|---|
| OS | Ubuntu 24.04.4 LTS (kernel 6.8.0-101-generic) |
| Hostname | <HOSTNAME> |
| IP | <SERVER_IP> |
| Docker | 29.3.0 |
| Docker Compose | v5.1.0 |
| Disk | 937GB NVMe, 26GB used (3%) |
| SSH User | <SSH_USER> |

### Listening Ports
| Port | Service |
|---|---|
| 22 | SSH |
| 25 | SMTP (docker-mailserver) |
| 53 | DNS stub resolver (127.0.0.54) |
| 443 | HTTPS (Caddy) |
| 465 | SMTPS (docker-mailserver) |
| 587 | SMTP Submission (docker-mailserver) |

### Docker Resource Usage
- Images: 12 (14.29GB)
- Containers: 22 total (11 running)
- Volumes: 19 (278.6MB)

---

## 2. Application Stack

### 2.1 Ghost CMS
- **Version**: 6.19.2
- **Node.js**: 22.22.0
- **Ghost CLI**: 1.28.4
- **Image**: ghost:latest
- **Internal Port**: 2368
- **URL**: https://howtoosoftware.com
- **Site UUID**: 10353e5c-60a4-4c5e-b118-0b7b370920e0
- **Active Theme**: howtoosoftware-custom (v2.0.1)
- **Content Format**: Lexical JSON (newer posts), Mobiledoc JSON (1 legacy post)

### 2.2 MySQL
- **Version**: 8.0.44
- **Container**: hts-db
- **User**: ghost
- **Password**: <REDACTED>
- **Root Password**: <REDACTED>
- **Databases**: ghost (78 tables), activitypub (16 tables)
- **Port**: NOT exposed to host (internal Docker network only)

### 2.3 Caddy (Reverse Proxy)
- **Version**: 2.10.2-alpine
- **Container**: hts-caddy
- **auto_https**: off (SSL handled upstream by Nginx Proxy Manager)
- **Ports**: 80, 443

#### Routes
| Domain/Path | Backend |
|---|---|
| `howtoosoftware.com` / `howto.software` | hts-ghost:2368 |
| `panel.howto.software` | <PTERODACTYL_IP>:80 (Pterodactyl) |
| `/.ghost/activitypub/*` | ap.ghost.org |
| `/.ghost/analytics/*` | hts-traffic-analytics:3000 |
| `/.ghost/tinybird/*` | hts-tinybird-local:7181 |

### 2.4 Tinybird Local (Analytics)
- **Image**: tinybirdco/tinybird-local:latest
- **Internal Port**: 7181
- **Backend**: ClickHouse
- **Backup**: Continuous 5-min MySQL backup loop → `tinybird_analytics_backup` table
- **Events Stored**: 158 analytics events
- **API Pipes**: 20+ (KPIs, trends, pages, sources, devices, locations, UTM, member activity, post engagement, hourly traffic, realtime dashboard)

### 2.5 ActivityPub
- **Version**: 1.1.0
- **Image**: ghcr.io/tryghost/activitypub
- **Database**: `activitypub` (16 tables: accounts, follows, likes, posts, feeds, notifications, outboxes, etc.)

### 2.6 Traffic Analytics
- **Version**: 1.0.23
- **Image**: ghost/traffic-analytics
- **Internal Port**: 3000

### 2.7 Docker Mailserver
- **Relay Mode**: Mailgun (smtp.mailgun.org:587)
- **Credentials**: <MAILGUN_SMTP_USERNAME> / <REDACTED>
- **Ports**: 25, 465, 587

### 2.8 Other Services
- **RustDesk Server**: Ports 21115-21119
- **hts-db-backup**: Separate container for DB backup

---

## 3. Ghost Configuration

### 3.1 Site Settings
| Setting | Value |
|---|---|
| title | howtosoftware |
| description | Thoughts, stories and ideas. |
| logo | https://howtoosoftware.com/content/images/2026/03/hts-logo-white.png |
| icon | __GHOST_URL__/content/images/2025/12/H2S_Thumbnail_White.png |
| cover_image | https://static.ghost.org/v5.0.0/images/publication-cover.jpg |
| accent_color | #FF1A75 |
| locale | en |
| timezone | America/Chicago |
| facebook | profile.php?id=61573529521286 |
| twitter | NULL |

### 3.2 Email / Newsletter Settings
| Setting | Value |
|---|---|
| mailgun_domain | mg.howtoosoftware.com |
| mailgun_api_key | <REDACTED> |
| mailgun_base_url | https://api.mailgun.net/v3 |
| email_track_clicks | true |
| email_track_opens | true |
| email_verification_required | false |

### 3.3 Members / Subscriptions
| Setting | Value |
|---|---|
| members_signup_access | all |
| default_content_visibility | public |
| members_track_sources | true |
| portal_button | false |
| portal_default_plan | yearly |
| portal_plans | ["free"] |
| Stripe connected | NO (all stripe keys NULL) |
| donations_currency | USD |
| donations_suggested_amount | 500 ($5.00) |

### 3.4 Feature Flags
| Feature | Enabled |
|---|---|
| ActivityPub | true |
| Unsplash | true |
| Pintura (image editor) | true |
| Explore | true |
| Comments | all |
| Outbound link tagging | true |
| Web analytics | true |
| IndexNow | true |
| Recommendations | false |
| Require email MFA | false |

### 3.5 Routes (routes.yaml)
```yaml
routes:

collections:
  /:
    permalink: /{slug}/
    template: index

taxonomies:
  tag: /tag/{slug}/
  author: /author/{slug}/
```

---

## 4. Content Data

### 4.1 Posts (9 published + 1 draft)
| Slug | Title | Status | Type | Lexical Len |
|---|---|---|---|---|
| why-wi-fi-over-ethernet | Why Wi-Fi Over Ethernet? | published | post | 2,602 |
| why-do-we-need-web-browsers | Why Do We Need Web Browsers? | published | post | 1,780 |
| early-days-of-technology | Early days of Technology | published | post | 1,859 |
| choosing-the-right-web-browser... | Choosing the Right Web Browser | published | post | 2,941 |
| why-choose-a-laptop-over-a-desktop | Why Choose a Laptop Over a Desktop? | published | post | 2,858 |
| what-phone-operating-system-is-for-you | What Phone OS is for You? | published | post | 2,999 |
| which-phone-is-best-for-you | Which Phone Is Best for You? | published | post | 6,560 |
| windows-11-optimization-guide | Windows 11 Optimization Guide | published | post | 7,774 |
| coming-soon | Coming soon | published | post | 0 (mobiledoc: 420) |
| hey-cutie | Hey cutie | draft | post | 176 |

### 4.2 Pages (9 published)
| Slug | Title | Lexical Len |
|---|---|---|
| setting-up-pzsm-windows... | Setting Up PZSM (Windows) | 16,060 |
| how-to-download-pzsm... | How to Download PZSM | 8,902 |
| overview-obzor | Overview | 15,387 |
| home | Home | 3,112 |
| public-project-zomboid...-archive | PZ Workshop Collection (Archive) | 537,006 |
| public-project-zomboid...-current | PZ Workshop Collection (Current) | 541,944 |
| airsoft-mixtape-alternate-media | Airsoft Mixtape | 2,679 |
| d-dog137-alternative-media | D dog137 | 4,445 |
| about | About this site | 2,982 |

### 4.3 Tags
- 1 tag: "News" (id: 6930f97d04b3d10001c01b72)
- Only 1 post tagged

### 4.4 Members (3)
| Email | Name | Status |
|---|---|---|
| <redacted> | <redacted> | free |
| <redacted> | <redacted> | free |
| <redacted> | <redacted> | free |

### 4.5 Users (1)
- <redacted> (author_id: 6930f97c04b3d10001c01b60) — sole author of all posts

### 4.6 Newsletter
- Name: howtoosoftware
- Slug: default-newsletter
- Status: active
- Subscribe on signup: yes
- Font: sans_serif (title & body)
- Title alignment: center
- Show feature image: yes
- Show badge: yes

### 4.7 Products/Tiers
| Name | Type | Price |
|---|---|---|
| Free | free | — |
| howtoosoftware | paid | $5/mo, $50/yr (USD) |

### 4.8 Email Campaigns (3 sent)
| Subject | Status | Count |
|---|---|---|
| Why Wi-Fi Over Ethernet? | submitting | 3 |
| Choosing the Right Web Browser... | submitting | 2 |
| Why Choose a Laptop Over a Desktop? | submitting | 2 |

---

## 5. API Keys

### Content API Keys
| Integration | Key ID | Secret |
|---|---|---|
| Ghost Internal Frontend | 6930f97d04b3d10001c01bfa | <REDACTED> |
| Ghost Core Content API | 6930f97d04b3d10001c01bfc | <REDACTED> |

### Admin API Keys
| Integration | Key ID | Secret |
|---|---|---|
| Zapier | 6930f97d04b3d10001c01bf0 | <REDACTED> |
| Ghost Explore | 6930f97d04b3d10001c01bf2 | <REDACTED> |
| Self-Serve Migration | 6930f97d04b3d10001c01bf4 | <REDACTED> |
| Ghost Backup | 6930f97d04b3d10001c01bf6 | <REDACTED> |
| Ghost Scheduler | 6930f97d04b3d10001c01bf8 | <REDACTED> |
| Transistor | 6976ab165dd3a50001be051c | <REDACTED> |

---

## 6. Integrations
| Name | Type |
|---|---|
| Zapier | builtin |
| Ghost Explore | core |
| Self-Serve Migration | core |
| Ghost Backup | internal |
| Ghost Scheduler | internal |
| Ghost Internal Frontend | internal |
| Ghost Core Content API | core |
| Ghost ActivityPub | internal |
| Transistor | builtin |

---

## 7. Theme Settings (howtoosoftware-custom)
| Setting | Type | Value |
|---|---|---|
| navigation_layout | select | Logo on cover |
| show_publication_cover | boolean | true |
| header_style | select | Center aligned |
| title_font | select | Modern sans-serif |
| body_font | select | Elegant serif |
| feed_layout | select | Classic |
| color_scheme | select | Light |
| post_image_style | select | Wide |
| email_signup_text | text | Sign up for more like this. |
| show_recent_posts_footer | boolean | true |

---

## 8. Tinybird Analytics Backup Schema
```sql
CREATE TABLE tinybird_analytics_backup (
  id BIGINT NOT NULL PRIMARY KEY AUTO_INCREMENT,
  timestamp DATETIME NOT NULL,
  session_id VARCHAR(255),
  action VARCHAR(100),
  version VARCHAR(50),
  payload MEDIUMTEXT,
  site_uuid VARCHAR(36),
  page_url TEXT,
  page_urlpath TEXT,
  referrer TEXT,
  device VARCHAR(100),
  browser VARCHAR(100),
  os VARCHAR(100),
  country VARCHAR(10),
  member_uuid VARCHAR(36),
  member_status VARCHAR(50),
  post_uuid VARCHAR(36),
  backed_up_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
-- 158 events stored
```

---

## 9. Uploaded Assets
- `/content/images/hts-logo-white.png` (18,825 bytes)
- `/content/images/2026/03/hts-logo-white.png` (copy)
- No other uploaded images

---

## 10. Security Notes
- ⚠️ **CVE Alert**: Ghost notification warns "your Ghost site is vulnerable to an attack that lets unauthenticated attackers read arbitrary data from the database" (CVE from Ghost v6.14.0)
- Ghost v6.19.2 should have the fix — update the notification dismissal
- MySQL password was weak and reused for the root and ghost users (value redacted from this doc)
- Mailgun credentials and API keys are stored in plaintext in the database settings
- MySQL and Tinybird ports correctly NOT exposed to host
- UFW firewall status unavailable (no sudo access for htsadmin)

---

## 11. Data Files Saved Locally
| File | Size | Contents |
|---|---|---|
| `DOCS/ghost_schema.sql` | 196KB | Full MySQL schema (CREATE TABLE for all 78 ghost + 16 activitypub tables) |
| `DOCS/ghost_posts_dump.sql` | 1.8MB | Complete posts table data (all lexical/mobiledoc content) |
| `DOCS/GHOST-DEPLOYMENT-DATA.md` | This file | Comprehensive deployment data |
| `REF/howtoosoftware-custom/` | — | Complete theme source code |
| `REF/howtoosoftware-custom/hts_compose_local.yml` | ~2500 lines | Full Docker Compose orchestration |
