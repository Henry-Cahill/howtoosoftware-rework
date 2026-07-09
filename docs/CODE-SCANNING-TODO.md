# Code Scanning TODO

Work list for the **78 open** GitHub CodeQL code-scanning alerts on
`Henry-Cahill/howtoosoftware-rework` (snapshot: 2026-07-06).

- Alerts dashboard: https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning
- Each item links to its alert as `#N`.
- Check items off as you fix them; a fix is verified once the alert flips to
  **Fixed** after the next CodeQL run on `main`.

## Summary

| Severity | Rule | Count |
| --- | --- | --- |
| High | `js/tainted-format-string` | 4 |
| High | `js/insecure-randomness` | 1 |
| Medium | `cs/log-forging` | 49 |
| Medium | `cs/exposure-of-sensitive-information` | 20 |
| Medium | `js/html-constructed-from-input` (vendored jQuery) | 2 |
| Medium | `js/unsafe-jquery-plugin` (vendored jQuery) | 2 |
| **Total** | | **78** |

## How to fix each rule

- **`cs/log-forging`** — user-controlled text flows into a log entry. Strip line
  breaks before logging, e.g. `value.Replace("\r", "").Replace("\n", "")`, or
  use structured logging with a sanitizing helper. Prefer a single shared
  `SanitizeForLog(...)` helper reused across services.
- **`cs/exposure-of-sensitive-information`** — private data (email addresses,
  result counts) is written to logs/output. Avoid logging raw emails; log a
  masked value or a stable hash/id instead.
- **`js/tainted-format-string`** — a user-provided value is used as a format
  string. Pass the value as an argument, never as the format template.
- **`js/insecure-randomness`** — `Math.random()` used in a security context.
  Use `crypto.getRandomValues()` instead.
- **`js/html-constructed-from-input` / `js/unsafe-jquery-plugin`** — these are in
  **vendored jQuery library files**. Preferred fix is to upgrade the library or
  dismiss as "used in tests / third-party" rather than editing vendor code.

---

## High severity — fix first (5)

### JavaScript

- [x] [src/HowToSoftware.Web/wwwroot/js/analytics.js](../src/HowToSoftware.Web/wwwroot/js/analytics.js#L113) L113 — `js/insecure-randomness` [#5](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/5)
- [x] [src/HowToSoftware.Web/wwwroot/js/main.js](../src/HowToSoftware.Web/wwwroot/js/main.js#L41) L41 — `js/tainted-format-string` [#8](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/8)
- [x] [src/HowToSoftware.Web/wwwroot/js/main.js](../src/HowToSoftware.Web/wwwroot/js/main.js#L43) L43 — `js/tainted-format-string` [#9](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/9)
- [x] [ref/howtoosoftware-custom/assets/js/main.js](../ref/howtoosoftware-custom/assets/js/main.js#L41) L41 — `js/tainted-format-string` [#6](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/6)
- [x] [ref/howtoosoftware-custom/assets/js/main.js](../ref/howtoosoftware-custom/assets/js/main.js#L43) L43 — `js/tainted-format-string` [#7](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/7)

---

## Medium severity — C# (69)

### HowToSoftware.Core

#### [src/HowToSoftware.Core/Services/AutomatedEmailService.cs](../src/HowToSoftware.Core/Services/AutomatedEmailService.cs)

- [x] L64 — `cs/log-forging` [#41](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/41), `cs/exposure-of-sensitive-information` [#19](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/19)
- [x] L99 — `cs/log-forging` [#42](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/42), `cs/exposure-of-sensitive-information` [#20](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/20)
- [x] L164 — `cs/exposure-of-sensitive-information` [#21](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/21)
- [x] L252 — `cs/log-forging` [#43](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/43), `cs/exposure-of-sensitive-information` [#22](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/22)
- [x] L275 — `cs/log-forging` [#44](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/44), `cs/exposure-of-sensitive-information` [#23](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/23)

#### [src/HowToSoftware.Core/Services/CommentService.cs](../src/HowToSoftware.Core/Services/CommentService.cs)

- [x] L58 — `cs/log-forging` [#30](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/30)
- [x] L81 — `cs/log-forging` [#31](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/31)
- [x] L97 — `cs/log-forging` [#32](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/32)
- [x] L123 — `cs/log-forging` [#33](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/33)
- [x] L136 — `cs/log-forging` [#34](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/34)
- [x] L162 — `cs/log-forging` [#35](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/35)

#### [src/HowToSoftware.Core/Services/MemberImportService.cs](../src/HowToSoftware.Core/Services/MemberImportService.cs)

- [x] L133 — `cs/exposure-of-sensitive-information` [#10](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/10)
- [x] L199 — `cs/exposure-of-sensitive-information` [#11](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/11)
- [x] L215 — `cs/exposure-of-sensitive-information` [#12](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/12)

#### [src/HowToSoftware.Core/Services/MemberService.cs](../src/HowToSoftware.Core/Services/MemberService.cs)

- [x] L77 — `cs/log-forging` [#45](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/45)

#### [src/HowToSoftware.Core/Services/SuppressionService.cs](../src/HowToSoftware.Core/Services/SuppressionService.cs)

- [x] L15 — `cs/exposure-of-sensitive-information` [#13](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/13), `cs/log-forging` [#36](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/36)
- [x] L23 — `cs/exposure-of-sensitive-information` [#14](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/14), `cs/log-forging` [#37](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/37)
- [x] L32 — `cs/exposure-of-sensitive-information` [#15](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/15)
- [x] L49 — `cs/exposure-of-sensitive-information` [#16](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/16), `cs/log-forging` [#38](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/38)
- [x] L70 — `cs/exposure-of-sensitive-information` [#17](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/17), `cs/log-forging` [#39](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/39)
- [x] L81 — `cs/exposure-of-sensitive-information` [#18](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/18), `cs/log-forging` [#40](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/40)

### HowToSoftware.Infrastructure

#### [src/HowToSoftware.Infrastructure/Services/CollectionService.cs](../src/HowToSoftware.Infrastructure/Services/CollectionService.cs)

- [x] L105 — `cs/log-forging` [#52](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/52)
- [x] L116 — `cs/log-forging` [#53](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/53)

#### [src/HowToSoftware.Infrastructure/Services/DonationService.cs](../src/HowToSoftware.Infrastructure/Services/DonationService.cs)

- [x] L79 — `cs/log-forging` [#46](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/46), [#47](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/47)
- [x] L118 — `cs/exposure-of-sensitive-information` [#24](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/24)

#### [src/HowToSoftware.Infrastructure/Services/MagicLinkService.cs](../src/HowToSoftware.Infrastructure/Services/MagicLinkService.cs)

- [x] L217 — `cs/log-forging` [#48](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/48)

#### [src/HowToSoftware.Infrastructure/Services/MailgunEmailService.cs](../src/HowToSoftware.Infrastructure/Services/MailgunEmailService.cs)

- [x] L66 — `cs/exposure-of-sensitive-information` [#26](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/26), `cs/log-forging` [#50](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/50)
- [x] L76 — `cs/exposure-of-sensitive-information` [#25](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/25), `cs/log-forging` [#49](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/49)
- [x] L123 — `cs/exposure-of-sensitive-information` [#27](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/27)
- [x] L192 — `cs/exposure-of-sensitive-information` [#28](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/28)
- [x] L201 — `cs/exposure-of-sensitive-information` [#29](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/29)

#### [src/HowToSoftware.Infrastructure/Services/MentionService.cs](../src/HowToSoftware.Infrastructure/Services/MentionService.cs)

- [x] L119 — `cs/log-forging` [#54](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/54), [#55](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/55)
- [x] L147 — `cs/log-forging` [#56](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/56), [#57](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/57)
- [x] L267 — `cs/log-forging` [#58](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/58)

#### [src/HowToSoftware.Infrastructure/Services/OfferService.cs](../src/HowToSoftware.Infrastructure/Services/OfferService.cs)

- [x] L139 — `cs/log-forging` [#59](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/59)
- [x] L162 — `cs/log-forging` [#61](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/61)
- [x] L167 — `cs/log-forging` [#60](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/60)
- [x] L172 — `cs/log-forging` [#62](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/62)

#### [src/HowToSoftware.Infrastructure/Services/RecommendationService.cs](../src/HowToSoftware.Infrastructure/Services/RecommendationService.cs)

- [x] L99 — `cs/log-forging` [#63](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/63)
- [x] L114 — `cs/log-forging` [#64](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/64)

#### [src/HowToSoftware.Infrastructure/Services/RedirectService.cs](../src/HowToSoftware.Infrastructure/Services/RedirectService.cs)

- [x] L96 — `cs/log-forging` [#65](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/65)

#### [src/HowToSoftware.Infrastructure/Services/SnippetService.cs](../src/HowToSoftware.Infrastructure/Services/SnippetService.cs)

- [x] L84 — `cs/log-forging` [#66](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/66)

#### [src/HowToSoftware.Infrastructure/Services/StripeService.cs](../src/HowToSoftware.Infrastructure/Services/StripeService.cs)

- [x] L181 — `cs/log-forging` [#67](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/67)
- [x] L210 — `cs/log-forging` [#68](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/68)
- [x] L266 — `cs/log-forging` [#69](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/69)
- [x] L458 — `cs/log-forging` [#70](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/70)

### HowToSoftware.Web

#### [src/HowToSoftware.Web/Controllers/DonationsController.cs](../src/HowToSoftware.Web/Controllers/DonationsController.cs)

- [x] L75 — `cs/log-forging` [#51](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/51)

#### [src/HowToSoftware.Web/Controllers/EmailWebhookController.cs](../src/HowToSoftware.Web/Controllers/EmailWebhookController.cs)

- [x] L62 — `cs/log-forging` [#71](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/71)
- [x] L96 — `cs/log-forging` [#72](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/72), [#73](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/73)

#### [src/HowToSoftware.Web/Controllers/OffersController.cs](../src/HowToSoftware.Web/Controllers/OffersController.cs)

- [x] L87 — `cs/log-forging` [#74](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/74)

#### [src/HowToSoftware.Web/Controllers/RecommendationsController.cs](../src/HowToSoftware.Web/Controllers/RecommendationsController.cs)

- [x] L59 — `cs/log-forging` [#75](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/75)
- [x] L84 — `cs/log-forging` [#76](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/76)

#### [src/HowToSoftware.Web/Controllers/WebmentionController.cs](../src/HowToSoftware.Web/Controllers/WebmentionController.cs)

- [x] L48 — `cs/log-forging` [#77](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/77), [#78](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/78)

---

## Medium severity — vendored jQuery (4)

Resolved by excluding the vendored library path
(`src/HowToSoftware.Web/wwwroot/lib/**`) from CodeQL analysis via
[.github/codeql/codeql-config.yml](../.github/codeql/codeql-config.yml)
(`paths-ignore`) together with advanced setup in
[.github/workflows/codeql.yml](../.github/workflows/codeql.yml). jQuery is
already on its latest release (3.7.1), so these library self-test /
plugin-scaffold findings cannot be fixed by upgrading and are false positives
in our code. The alerts close on the next CodeQL run once the repository is
switched from default to **Advanced** code-scanning setup
(Settings → Code security → Code scanning).

#### [src/HowToSoftware.Web/wwwroot/lib/jquery/dist/jquery.js](../src/HowToSoftware.Web/wwwroot/lib/jquery/dist/jquery.js)

- [x] L1279 — `js/html-constructed-from-input` [#3](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/3)
- [x] L1280 — `js/html-constructed-from-input` [#4](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/4)

#### [src/HowToSoftware.Web/wwwroot/lib/jquery-validation/dist/jquery.validate.js](../src/HowToSoftware.Web/wwwroot/lib/jquery-validation/dist/jquery.validate.js)

- [x] L689 — `js/unsafe-jquery-plugin` [#1](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/1)
- [x] L1086 — `js/unsafe-jquery-plugin` [#2](https://github.com/Henry-Cahill/howtoosoftware-rework/security/code-scanning/2)
