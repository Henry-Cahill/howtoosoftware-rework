# HowToSoftware Clone — .NET 10 / C# / T-SQL Architecture
## Replacing Ghost CMS with a custom-built platform

---

## 1. Technology Mapping

| Ghost Stack | .NET 10 Clone |
|---|---|
| Node.js / Ghost CMS | ASP.NET Core 10 (C#) |
| MySQL 8.0 | SQL Server 2025 (T-SQL) |
| Handlebars templates | Razor Pages / Razor Components |
| Lexical/Mobiledoc JSON | Custom Lexical-to-HTML renderer |
| Ghost Content API | ASP.NET Web API (REST) |
| Ghost Admin API | ASP.NET Web API + Blazor Admin |
| Caddy reverse proxy | YARP / Caddy / Nginx |
| Tinybird/ClickHouse analytics | Custom analytics (SQL Server + SignalR) |
| docker-mailserver + Mailgun | MailKit + Mailgun relay |
| ActivityPub | Custom ActivityPub implementation |
| Ghost Members/Auth | ASP.NET Identity |
| npm/Gulp theme build | dotnet build + Vite/esbuild |

---

## 2. Solution Structure

```
HowToSoftware/
├── docker-compose.yml
├── docker-compose.override.yml
├── .env
│
├── src/
│   ├── HowToSoftware.Web/              # Main web application (Razor Pages)
│   │   ├── Program.cs
│   │   ├── Pages/
│   │   │   ├── Index.cshtml             # Home / post feed (replaces index.hbs)
│   │   │   ├── Post.cshtml              # Single post (replaces post.hbs)
│   │   │   ├── Page.cshtml              # Static page (replaces page.hbs)
│   │   │   ├── Author.cshtml            # Author archive (replaces author.hbs)
│   │   │   ├── Tag.cshtml               # Tag archive (replaces tag.hbs)
│   │   │   ├── Error/
│   │   │   │   ├── 400.cshtml
│   │   │   │   ├── 401.cshtml
│   │   │   │   ├── 403.cshtml
│   │   │   │   ├── 404.cshtml
│   │   │   │   └── 500.cshtml
│   │   │   └── Shared/
│   │   │       ├── _Layout.cshtml       # replaces default.hbs
│   │   │       ├── _PostCard.cshtml     # replaces partials/post-card.hbs
│   │   │       ├── _Pagination.cshtml   # replaces partials/pagination.hbs
│   │   │       └── _Lightbox.cshtml     # replaces partials/lightbox.hbs
│   │   ├── wwwroot/
│   │   │   ├── css/screen.css
│   │   │   ├── js/main.js
│   │   │   └── images/
│   │   └── Controllers/
│   │       └── ContentApiController.cs  # Ghost Content API equivalent
│   │
│   ├── HowToSoftware.Admin/            # Admin panel (Blazor Server or WASM)
│   │   ├── Program.cs
│   │   ├── Pages/
│   │   │   ├── Dashboard.razor
│   │   │   ├── Posts/
│   │   │   │   ├── PostList.razor
│   │   │   │   └── PostEditor.razor     # Lexical/rich-text editor
│   │   │   ├── Pages/
│   │   │   ├── Tags/
│   │   │   ├── Members/
│   │   │   ├── Newsletters/
│   │   │   ├── Settings/
│   │   │   └── Analytics/
│   │   └── Components/
│   │
│   ├── HowToSoftware.Core/             # Domain models & interfaces
│   │   ├── Entities/
│   │   │   ├── Post.cs
│   │   │   ├── Tag.cs
│   │   │   ├── Author.cs
│   │   │   ├── Member.cs
│   │   │   ├── Newsletter.cs
│   │   │   ├── Product.cs
│   │   │   ├── Email.cs
│   │   │   ├── Integration.cs
│   │   │   ├── Setting.cs
│   │   │   └── AnalyticsEvent.cs
│   │   ├── Interfaces/
│   │   │   ├── IPostRepository.cs
│   │   │   ├── IMemberRepository.cs
│   │   │   ├── IAnalyticsService.cs
│   │   │   ├── IEmailService.cs
│   │   │   └── IContentRenderer.cs
│   │   └── Services/
│   │       ├── LexicalRenderer.cs       # Lexical JSON → HTML
│   │       ├── MobiledocRenderer.cs     # Mobiledoc JSON → HTML (legacy)
│   │       └── SlugGenerator.cs
│   │
│   ├── HowToSoftware.Infrastructure/   # Data access & external services
│   │   ├── Data/
│   │   │   ├── AppDbContext.cs          # EF Core DbContext
│   │   │   ├── Configurations/          # Entity type configs
│   │   │   └── Migrations/
│   │   ├── Repositories/
│   │   │   ├── PostRepository.cs
│   │   │   └── MemberRepository.cs
│   │   ├── Services/
│   │   │   ├── MailgunEmailService.cs
│   │   │   ├── AnalyticsService.cs
│   │   │   └── ImageStorageService.cs
│   │   └── ActivityPub/                 # ActivityPub protocol
│   │       ├── ActivityPubService.cs
│   │       ├── InboxController.cs
│   │       └── OutboxController.cs
│   │
│   └── HowToSoftware.Migrator/         # Ghost → .NET data migration tool
│       ├── Program.cs
│       ├── GhostDataImporter.cs
│       ├── LexicalContentConverter.cs
│       └── MemberImporter.cs
│
├── tests/
│   ├── HowToSoftware.Core.Tests/
│   ├── HowToSoftware.Web.Tests/
│   └── HowToSoftware.Infrastructure.Tests/
│
└── deploy/
    ├── Dockerfile.web
    ├── Dockerfile.admin
    └── caddy/
        └── Caddyfile
```

---

## 3. Database Schema (T-SQL)

### 3.1 Key Design Decisions
- Ghost uses `VARCHAR(24)` hex IDs → Switch to `UNIQUEIDENTIFIER` (GUID) or keep varchar(24) for migration compatibility
- Ghost uses `utf8mb4` → SQL Server `NVARCHAR` (native Unicode)
- Ghost's `longtext` → `NVARCHAR(MAX)`
- Ghost's `tinyint(1)` booleans → `BIT`
- Ghost's `datetime` → `DATETIME2(7)`

### 3.2 Core Tables

```sql
-- Posts (maps Ghost's 'posts' table)
CREATE TABLE [dbo].[Posts] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Uuid]              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    [Title]             NVARCHAR(2000)      NOT NULL,
    [Slug]              NVARCHAR(191)       NOT NULL UNIQUE,
    [Lexical]           NVARCHAR(MAX)       NULL,      -- Lexical JSON content
    [Html]              NVARCHAR(MAX)       NULL,      -- Rendered HTML
    [PlainText]         NVARCHAR(MAX)       NULL,
    [FeatureImage]      NVARCHAR(2000)      NULL,
    [Featured]          BIT                 NOT NULL DEFAULT 0,
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'draft',  -- draft, published, scheduled, sent
    [Type]              NVARCHAR(50)        NOT NULL DEFAULT 'post',   -- post, page
    [Visibility]        NVARCHAR(50)        NOT NULL DEFAULT 'public', -- public, members, paid, tiers
    [CustomExcerpt]     NVARCHAR(2000)      NULL,
    [MetaTitle]         NVARCHAR(2000)      NULL,
    [MetaDescription]   NVARCHAR(2000)      NULL,
    [OgTitle]           NVARCHAR(300)       NULL,
    [OgDescription]     NVARCHAR(500)       NULL,
    [OgImage]           NVARCHAR(2000)      NULL,
    [TwitterTitle]      NVARCHAR(300)       NULL,
    [TwitterDescription] NVARCHAR(500)      NULL,
    [TwitterImage]      NVARCHAR(2000)      NULL,
    [EmailRecipientFilter] NVARCHAR(MAX)    NOT NULL DEFAULT 'all',
    [PublishedAt]       DATETIME2           NULL,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [CreatedById]       NVARCHAR(24)        NOT NULL,
    [UpdatedById]       NVARCHAR(24)        NOT NULL
);

-- Authors/Users
CREATE TABLE [dbo].[Users] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Name]              NVARCHAR(191)       NOT NULL,
    [Slug]              NVARCHAR(191)       NOT NULL UNIQUE,
    [Email]             NVARCHAR(191)       NOT NULL UNIQUE,
    [PasswordHash]      NVARCHAR(MAX)       NULL,  -- ASP.NET Identity hash
    [ProfileImage]      NVARCHAR(2000)      NULL,
    [CoverImage]        NVARCHAR(2000)      NULL,
    [Bio]               NVARCHAR(MAX)       NULL,
    [Website]           NVARCHAR(2000)      NULL,
    [Location]          NVARCHAR(150)       NULL,
    [Facebook]          NVARCHAR(2000)      NULL,
    [Twitter]           NVARCHAR(2000)      NULL,
    [Accessibility]     NVARCHAR(MAX)       NULL,
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'active',
    [Locale]            NVARCHAR(6)         NULL,
    [Visibility]        NVARCHAR(50)        NOT NULL DEFAULT 'public',
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Tags
CREATE TABLE [dbo].[Tags] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Name]              NVARCHAR(191)       NOT NULL,
    [Slug]              NVARCHAR(191)       NOT NULL UNIQUE,
    [Description]       NVARCHAR(MAX)       NULL,
    [FeatureImage]      NVARCHAR(2000)      NULL,
    [Visibility]        NVARCHAR(50)        NOT NULL DEFAULT 'public',
    [MetaTitle]         NVARCHAR(2000)      NULL,
    [MetaDescription]   NVARCHAR(2000)      NULL,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Posts ↔ Tags (many-to-many)
CREATE TABLE [dbo].[PostsTags] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [PostId]            NVARCHAR(24)        NOT NULL REFERENCES [Posts]([Id]),
    [TagId]             NVARCHAR(24)        NOT NULL REFERENCES [Tags]([Id]),
    [SortOrder]         INT                 NOT NULL DEFAULT 0
);

-- Posts ↔ Authors (many-to-many)
CREATE TABLE [dbo].[PostsAuthors] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [PostId]            NVARCHAR(24)        NOT NULL REFERENCES [Posts]([Id]),
    [AuthorId]          NVARCHAR(24)        NOT NULL REFERENCES [Users]([Id]),
    [SortOrder]         INT                 NOT NULL DEFAULT 0
);

-- Members
CREATE TABLE [dbo].[Members] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Uuid]              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    [Email]             NVARCHAR(191)       NOT NULL UNIQUE,
    [Name]              NVARCHAR(191)       NULL,
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'free', -- free, paid, comped
    [Note]              NVARCHAR(2000)      NULL,
    [GeolocationHeaderText] NVARCHAR(MAX)   NULL,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Newsletters
CREATE TABLE [dbo].[Newsletters] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Uuid]              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    [Name]              NVARCHAR(191)       NOT NULL,
    [Slug]              NVARCHAR(191)       NOT NULL UNIQUE,
    [SenderName]        NVARCHAR(191)       NULL,
    [SenderEmail]       NVARCHAR(191)       NULL,
    [SenderReplyTo]     NVARCHAR(50)        NOT NULL DEFAULT 'newsletter',
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'active',
    [Visibility]        NVARCHAR(50)        NOT NULL DEFAULT 'members',
    [SubscribeOnSignup] BIT                 NOT NULL DEFAULT 1,
    [SortOrder]         INT                 NOT NULL DEFAULT 0,
    [ShowHeaderIcon]    BIT                 NOT NULL DEFAULT 1,
    [ShowHeaderTitle]   BIT                 NOT NULL DEFAULT 1,
    [TitleFontCategory] NVARCHAR(191)       NOT NULL DEFAULT 'sans_serif',
    [TitleAlignment]    NVARCHAR(191)       NOT NULL DEFAULT 'center',
    [ShowFeatureImage]  BIT                 NOT NULL DEFAULT 1,
    [BodyFontCategory]  NVARCHAR(191)       NOT NULL DEFAULT 'sans_serif',
    [ShowBadge]         BIT                 NOT NULL DEFAULT 1,
    [FeedbackEnabled]   BIT                 NOT NULL DEFAULT 0,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Products/Tiers
CREATE TABLE [dbo].[Products] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Name]              NVARCHAR(191)       NOT NULL,
    [Slug]              NVARCHAR(191)       NOT NULL UNIQUE,
    [Type]              NVARCHAR(50)        NOT NULL DEFAULT 'paid', -- free, paid
    [Active]            BIT                 NOT NULL DEFAULT 1,
    [Visibility]        NVARCHAR(50)        NOT NULL DEFAULT 'public',
    [TrialDays]         INT                 NOT NULL DEFAULT 0,
    [Currency]          NVARCHAR(10)        NULL,
    [MonthlyPrice]      INT                 NULL, -- cents
    [YearlyPrice]       INT                 NULL, -- cents
    [Description]       NVARCHAR(191)       NULL,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Emails (campaign tracking)
CREATE TABLE [dbo].[Emails] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Uuid]              UNIQUEIDENTIFIER    NOT NULL DEFAULT NEWID(),
    [PostId]            NVARCHAR(24)        NOT NULL REFERENCES [Posts]([Id]),
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'pending',
    [RecipientFilter]   NVARCHAR(MAX)       NOT NULL DEFAULT 'all',
    [EmailCount]        INT                 NOT NULL DEFAULT 0,
    [DeliveredCount]    INT                 NOT NULL DEFAULT 0,
    [OpenedCount]       INT                 NOT NULL DEFAULT 0,
    [FailedCount]       INT                 NOT NULL DEFAULT 0,
    [Subject]           NVARCHAR(300)       NULL,
    [Html]              NVARCHAR(MAX)       NULL,
    [PlainText]         NVARCHAR(MAX)       NULL,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Settings (key-value store like Ghost)
CREATE TABLE [dbo].[Settings] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [Key]               NVARCHAR(191)       NOT NULL UNIQUE,
    [Value]             NVARCHAR(MAX)       NULL,
    [Group]             NVARCHAR(191)       NOT NULL DEFAULT 'site',
    [Type]              NVARCHAR(50)        NOT NULL DEFAULT 'string',
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);

-- Analytics Events (replaces Tinybird)
CREATE TABLE [dbo].[AnalyticsEvents] (
    [Id]                BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [Timestamp]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [SessionId]         NVARCHAR(255)       NULL,
    [Action]            NVARCHAR(100)       NULL,
    [Version]           NVARCHAR(50)        NULL,
    [Payload]           NVARCHAR(MAX)       NULL,
    [SiteUuid]          NVARCHAR(36)        NULL,
    [PageUrl]           NVARCHAR(MAX)       NULL,
    [PageUrlPath]       NVARCHAR(MAX)       NULL,
    [Referrer]          NVARCHAR(MAX)       NULL,
    [Device]            NVARCHAR(100)       NULL,
    [Browser]           NVARCHAR(100)       NULL,
    [Os]                NVARCHAR(100)       NULL,
    [Country]           NVARCHAR(10)        NULL,
    [MemberUuid]        NVARCHAR(36)        NULL,
    [MemberStatus]      NVARCHAR(50)        NULL,
    [PostUuid]          NVARCHAR(36)        NULL,
    INDEX IX_AnalyticsEvents_Timestamp ([Timestamp]),
    INDEX IX_AnalyticsEvents_SessionId ([SessionId]),
    INDEX IX_AnalyticsEvents_SiteUuid ([SiteUuid])
);

-- Comments
CREATE TABLE [dbo].[Comments] (
    [Id]                NVARCHAR(24)        NOT NULL PRIMARY KEY,
    [PostId]            NVARCHAR(24)        NOT NULL REFERENCES [Posts]([Id]),
    [MemberId]          NVARCHAR(24)        NOT NULL REFERENCES [Members]([Id]),
    [ParentId]          NVARCHAR(24)        NULL REFERENCES [Comments]([Id]),
    [Status]            NVARCHAR(50)        NOT NULL DEFAULT 'published',
    [Html]              NVARCHAR(MAX)       NULL,
    [CreatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME(),
    [UpdatedAt]         DATETIME2           NOT NULL DEFAULT SYSUTCDATETIME()
);
```

---

## 4. Docker Compose (Clone)

```yaml
services:
  # SQL Server 2025
  db:
    image: mcr.microsoft.com/mssql/server:2025-latest
    container_name: hts-db
    environment:
      ACCEPT_EULA: "Y"
      MSSQL_SA_PASSWORD: "${DB_SA_PASSWORD}"
      MSSQL_PID: Developer
    volumes:
      - hts-db-data:/var/opt/mssql
    expose:
      - "1433"
    healthcheck:
      test: /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "${DB_SA_PASSWORD}" -C -Q "SELECT 1"
      interval: 10s
      timeout: 5s
      retries: 5
    restart: unless-stopped

  # Main Web App (ASP.NET Core 10)
  web:
    build:
      context: .
      dockerfile: deploy/Dockerfile.web
    container_name: hts-web
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: "http://+:5000"
      ConnectionStrings__DefaultConnection: "Server=db;Database=HowToSoftware;User=sa;Password=${DB_SA_PASSWORD};TrustServerCertificate=True"
      Mail__MailgunDomain: "${MAILGUN_DOMAIN}"
      Mail__MailgunApiKey: "${MAILGUN_API_KEY}"
      Site__Url: "https://howtoosoftware.com"
      Site__Title: "howtosoftware"
    volumes:
      - hts-content:/app/wwwroot/content
    depends_on:
      db:
        condition: service_healthy
    expose:
      - "5000"
    restart: unless-stopped

  # Admin Panel (Blazor)
  admin:
    build:
      context: .
      dockerfile: deploy/Dockerfile.admin
    container_name: hts-admin
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: "http://+:5001"
      ConnectionStrings__DefaultConnection: "Server=db;Database=HowToSoftware;User=sa;Password=${DB_SA_PASSWORD};TrustServerCertificate=True"
    depends_on:
      db:
        condition: service_healthy
    expose:
      - "5001"
    restart: unless-stopped

  # Reverse Proxy
  caddy:
    image: caddy:2-alpine
    container_name: hts-caddy
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./deploy/caddy/Caddyfile:/etc/caddy/Caddyfile
      - hts-caddy-data:/data
      - hts-caddy-config:/config
    depends_on:
      - web
      - admin
    restart: unless-stopped

volumes:
  hts-db-data:
  hts-content:
  hts-caddy-data:
  hts-caddy-config:
```

---

## 5. Key Implementation Areas

### 5.1 Content Rendering Pipeline
Ghost stores content as **Lexical JSON** (editor state). The clone needs:
1. **LexicalRenderer**: Parse Lexical JSON → produce HTML for display
2. **MobiledocRenderer**: Parse legacy Mobiledoc JSON → HTML (for the 1 old post)
3. Rich text editor in admin (use Lexical.js or a Blazor-compatible editor)

### 5.2 Content API (REST)
Replicate Ghost's Content API endpoints:
- `GET /api/content/posts/` — List posts with filtering, pagination, includes
- `GET /api/content/posts/{id_or_slug}/` — Single post
- `GET /api/content/pages/` — List pages
- `GET /api/content/tags/` — List tags
- `GET /api/content/authors/` — List authors
- `GET /api/content/settings/` — Site settings
- Authentication via `?key={content_api_key}` query param

### 5.3 Admin API
- CRUD for posts, pages, tags, members, newsletters
- Image upload to `/content/images/`
- Settings management
- JWT-based admin auth (ASP.NET Identity)
- Ghost JSON import/export compatibility

### 5.4 Email / Newsletter System
- **MailKit** for SMTP sending via Mailgun
- Newsletter template rendering (Razor → HTML email)
- Track opens (pixel tracking) and clicks (link wrapping)
- Per-member subscription management
- Batch sending with rate limiting

### 5.5 Analytics (replacing Tinybird)
- JavaScript tracking snippet (page views, sessions, actions)
- `POST /api/analytics/event` — ingest endpoint
- SQL Server stored procedures for aggregation:
  - KPIs (visitors, visits, pageviews, bounce rate, avg session duration)
  - Top pages, sources, referrers
  - Device/browser/OS/country breakdowns
  - UTM campaign tracking
  - Member activity correlation
- SignalR for real-time dashboard updates

### 5.6 Member Portal
- Magic link email authentication (like Ghost)
- Free member signup
- Paid tier management (Stripe integration - future)
- Member-only content gating
- Comment system (authenticated via member session)

### 5.7 Theme/Front-End
The current Ghost theme maps to Razor Pages:
- `default.hbs` → `_Layout.cshtml`: Dark mode, progress bar, back-to-top, responsive nav
- `index.hbs` → `Index.cshtml`: Hero section + 3-column post grid
- `post.hbs` → `Post.cshtml`: Article with tags, srcset images, related posts, comments
- `page.hbs` → `Page.cshtml`: Static page with optional feature image
- `author.hbs` → `Author.cshtml`: Author profile + filtered post feed
- `tag.hbs` → `Tag.cshtml`: Tag description + filtered post feed
- CSS: Keep `screen.css` (CSS variables, dark mode, Koenig card styles)
- JS: Keep `main.js` (dark mode, progress bar, code copy, TOC, lazy load)

---

## 6. Migration Plan

### Phase 1: Foundation
1. Create solution structure and projects
2. Define EF Core entity models from Ghost schema
3. Create SQL Server migrations
4. Set up Docker Compose with SQL Server
5. Implement basic Razor Pages layout from Ghost theme

### Phase 2: Core Features
6. Build Content API (posts, pages, tags, authors)
7. Implement Lexical JSON → HTML renderer
8. Build post/page display pages
9. Import Ghost data using Migrator tool
10. Implement image upload/storage

### Phase 3: Members & Email
11. ASP.NET Identity for members (magic link auth)
12. Newsletter system with Mailgun integration
13. Content gating (public/members/paid)
14. Comment system

### Phase 4: Admin & Analytics
15. Blazor admin panel (post editor, settings)
16. Analytics tracking and dashboard
17. SEO features (meta, sitemap, robots.txt)

### Phase 5: Advanced
18. ActivityPub federation
19. Stripe integration for paid tiers
20. Search functionality
21. RSS feed generation
