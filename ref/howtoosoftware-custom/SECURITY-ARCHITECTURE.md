# HowTooSoftware Security Architecture

## Infrastructure Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                              INTERNET                                            │
│                                                                                  │
│    Shodan/Bots constantly scan for:                                             │
│    - Open admin panels (Portainer, Webmin, etc.)                                │
│    - Exposed databases (MySQL 3306, Postgres 5432)                              │
│    - Management interfaces (9000, 9443, etc.)                                   │
└────────────────────────────────┬────────────────────────────────────────────────┘
                                 │
                                 │ Only ports 80 & 443 forwarded
                                 ▼
┌─────────────────────────────────────────────────────────────────────────────────┐
│                         ROUTER / FIREWALL                                        │
│                                                                                  │
│  Port Forwarding:                                                               │
│  ├── 80  → 192.168.1.102:80   (Nginx Proxy Manager HTTP)                       │
│  ├── 443 → 192.168.1.102:443  (Nginx Proxy Manager HTTPS)                      │
│  └── 25  → 192.168.1.105:25   (Mail server - if receiving external mail)       │
│                                                                                  │
│  BLOCKED from external:                                                         │
│  ├── 3306  (MySQL)                                                              │
│  ├── 7181  (Tinybird API)                                                       │
│  ├── 8123  (ClickHouse)                                                         │
│  ├── 9000  (Portainer - NEVER expose directly)                                  │
│  ├── 81   (NPM Admin - NEVER expose directly)                                   │
│  └── 587/993 (SMTP submission/IMAP)                                             │
└─────────────────────────────────┬───────────────────────────────────────────────┘
                                  │
          ┌───────────────────────┴───────────────────────┐
          │                                               │
          ▼                                               ▼
┌─────────────────────────────────┐     ┌─────────────────────────────────────────┐
│  Website Server                 │     │  HowTooSoftware Server                  │
│  192.168.1.102                  │     │  192.168.1.105                          │
│                                 │     │                                         │
│  ┌───────────────────────────┐  │     │  ┌─────────────────────────────────┐   │
│  │ Nginx Proxy Manager       │  │────▶│  │ Caddy (hts-caddy)               │   │
│  │ Ports: 80, 443 (public)   │  │     │  │ Port: 80, 443 (internal)        │   │
│  │ Admin: 81 (internal only) │  │     │  │ Reverse proxy for Ghost         │   │
│  │                           │  │     │  └─────────────────────────────────┘   │
│  │ Routes:                   │  │     │                 │                       │
│  │ ├─ howtoosoftware.com     │  │     │                 ▼                       │
│  │ │  → 192.168.1.105:80     │  │     │  ┌─────────────────────────────────┐   │
│  │ ├─ portainer.yourdomain   │  │     │  │ Ghost (hts-ghost)               │   │
│  │ │  → localhost:9000       │  │     │  │ Port: 2368 (internal only)      │   │
│  │ │  ACCESS LIST: Admin IPs │  │     │  └─────────────────────────────────┘   │
│  │ └─ npm.yourdomain         │  │     │                 │                       │
│  │    → localhost:81         │  │     │                 ▼                       │
│  │    ACCESS LIST: Admin IPs │  │     │  ┌─────────────────────────────────┐   │
│  └───────────────────────────┘  │     │  │ MySQL (hts-db)                  │   │
│                                 │     │  │ Port: 3306 (internal only)      │   │
│  ┌───────────────────────────┐  │     │  └─────────────────────────────────┘   │
│  │ Portainer                 │  │     │                 │                       │
│  │ Port: 9000 (internal)     │  │     │                 ▼                       │
│  │ NEVER exposed directly!   │  │     │  ┌─────────────────────────────────┐   │
│  └───────────────────────────┘  │     │  │ Tinybird Local (hts-tinybird)   │   │
│                                 │     │  │ Ports: 7181, 8001, 8123          │   │
└─────────────────────────────────┘     │  │ (internal only)                 │   │
                                        │  └─────────────────────────────────┘   │
                                        │                                         │
                                        │  ┌─────────────────────────────────┐   │
                                        │  │ Mailserver (hts-mailserver)     │   │
                                        │  │ Port: 25 (if receiving mail)    │   │
                                        │  │ 587 (relay - internal)          │   │
                                        │  └─────────────────────────────────┘   │
                                        └─────────────────────────────────────────┘
```

## Why This Matters

### The Shodan Problem

Services like [Shodan](https://www.shodan.io) continuously scan the entire internet and index:
- Open ports and services
- Software versions and banners
- Known vulnerabilities

**Example:** `https://www.shodan.io/host/75.34.225.16`

When a vulnerability is discovered (CVE), attackers:
1. Query Shodan for all hosts running that software
2. Launch automated attacks within **minutes**
3. Your server can be compromised before you even hear about the CVE

### Services Attackers Love to Find

| Service | Default Port | Risk if Exposed |
|---------|-------------|-----------------|
| Portainer | 9000, 9443 | Full container control = full server control |
| MySQL | 3306 | Database access, data theft, ransomware |
| Redis | 6379 | Often no auth, command execution |
| Elasticsearch | 9200 | Data theft, cryptojacking |
| Docker API | 2375 | Container escape, root access |
| NPM Admin | 81 | Proxy configuration = redirect traffic |

## Nginx Proxy Manager Security Setup

### 1. Access Lists Configuration

In NPM, create an Access List for admin services:

1. **Go to:** Access Lists → Add Access List
2. **Name:** `Admin Only`
3. **Authorization:** Add your IP(s) or use basic auth
4. **Access:** Add allowed IP ranges:
   ```
   192.168.1.0/24    # Local network
   YOUR.PUBLIC.IP    # Your home/office IP
   ```

### 2. Proxy Host Configuration for Portainer

```
Domain:     portainer.yourdomain.com
Scheme:     http
Forward IP: 192.168.1.102  (or localhost if same server)
Port:       9000

SSL Tab:
  ☑ Force SSL
  ☑ HTTP/2 Support
  
Advanced Tab:
  Access List: Admin Only
```

### 3. Proxy Host Configuration for NPM Admin

```
Domain:     npm.yourdomain.com  
Scheme:     http
Forward IP: localhost
Port:       81

SSL Tab:
  ☑ Force SSL
  ☑ HTTP/2 Support
  
Advanced Tab:
  Access List: Admin Only
```

### 4. Proxy Host for HowTooSoftware

```
Domain:     howtoosoftware.com
Scheme:     http
Forward IP: 192.168.1.105
Port:       80

SSL Tab:
  ☑ Force SSL
  ☑ HTTP/2 Support
  ☑ HSTS Enabled
```

## Docker Compose Security Changes

The `hts_compose_local.yml` has been updated to:

### ✅ Internal-Only Services (No Host Port Binding)

```yaml
# MySQL - SECURED
db:
  # ports:
  #   - "3306:3306"  # REMOVED
  expose:
    - "3306"  # Container network only

# Tinybird - SECURED  
tinybird-local:
  # ports:
  #   - "7181:7181"  # REMOVED
  #   - "8001:8001"  # REMOVED
  #   - "8123:8123"  # REMOVED
  expose:
    - "7181"
    - "8001"
    - "8123"
```

### ⚠️ Still Exposed (Required for Function)

```yaml
# Caddy - Exposed (receives traffic from NPM)
caddy:
  ports:
    - "80:80"    # HTTP redirect
    - "443:443"  # HTTPS (local SSL)

# Mailserver - Exposed (if receiving external email)
mailserver:
  ports:
    - "25:25"    # SMTP inbound
```

## Firewall Rules (UFW Example)

On the HowTooSoftware server (192.168.1.105):

```bash
# Default deny incoming
sudo ufw default deny incoming
sudo ufw default allow outgoing

# Allow SSH from local network only
sudo ufw allow from 192.168.1.0/24 to any port 22

# Allow HTTP/HTTPS from NPM server
sudo ufw allow from 192.168.1.102 to any port 80
sudo ufw allow from 192.168.1.102 to any port 443

# Allow SMTP inbound (if receiving mail)
sudo ufw allow 25/tcp

# Enable firewall
sudo ufw enable
```

## Verification Checklist

### Before Going Live

- [ ] Run `nmap -sS -p- your.public.ip` from external network
- [ ] Only ports 80, 443, 25 (if mail) should be open
- [ ] Test Shodan: `https://www.shodan.io/host/YOUR.IP`
- [ ] Verify admin panels require authentication
- [ ] Test access lists block unauthorized IPs

### Regular Maintenance

- [ ] Review Shodan results monthly
- [ ] Update containers when security patches release
- [ ] Rotate credentials periodically
- [ ] Review NPM access logs for suspicious activity
- [ ] Keep backup of NPM configuration

## Quick Reference

| What | Where | Access |
|------|-------|--------|
| Ghost Blog | https://howtoosoftware.com | Public |
| Ghost Admin | https://howtoosoftware.com/ghost | Public (Ghost auth) |
| Portainer | https://portainer.yourdomain.com | Admin IPs only |
| NPM Admin | https://npm.yourdomain.com | Admin IPs only |
| MySQL | hts-db:3306 | Container network only |
| Tinybird | hts-tinybird-local:7181 | Container network only |

## Emergency Response

If you suspect compromise:

1. **Isolate:** Disconnect server from network
2. **Preserve:** Don't reboot - capture memory state
3. **Investigate:** Check container logs, access logs
4. **Rotate:** All credentials, API keys, tokens
5. **Rebuild:** From known-good backups if needed

---

*Last updated: January 2026*
*Architecture designed for howtoosoftware.com infrastructure*
