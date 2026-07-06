# Third-Party Notices

**HowToSoftware** incorporates third-party software and data. The components
below are the property of their respective owners and are used under the
licenses indicated. This file is provided to satisfy the attribution and
notice requirements of those licenses; it does **not** grant any rights in the
first-party HowToSoftware source code, which is governed by [LICENSE](LICENSE).

> License identifiers are provided in good faith. For the authoritative license
> text and copyright notices, consult each component's own distribution
> (the `LICENSE`/`NOTICE` file in its package or repository).

## NuGet packages

| Package | Version | License | Project |
|---|---|---|---|
| Microsoft.AspNetCore.SignalR.Client | 10.0.4 | MIT | https://www.nuget.org/packages/Microsoft.AspNetCore.SignalR.Client |
| Microsoft.EntityFrameworkCore | 10.0.5 | MIT | https://www.nuget.org/packages/Microsoft.EntityFrameworkCore |
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.5 | MIT | https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.SqlServer |
| Microsoft.EntityFrameworkCore.Design | 10.0.5 | MIT | https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Design |
| Microsoft.EntityFrameworkCore.Tools | 10.0.5 | MIT | https://www.nuget.org/packages/Microsoft.EntityFrameworkCore.Tools |
| Microsoft.AspNetCore.Identity.EntityFrameworkCore | 10.0.5 | MIT | https://www.nuget.org/packages/Microsoft.AspNetCore.Identity.EntityFrameworkCore |
| SixLabors.ImageSharp | 3.1.12 | Six Labors Split License (Apache-2.0 for OSS use) | https://www.nuget.org/packages/SixLabors.ImageSharp |
| HtmlSanitizer | 9.0.892 | MIT | https://www.nuget.org/packages/HtmlSanitizer |
| MaxMind.GeoIP2 | 5.2.0 | Apache-2.0 | https://www.nuget.org/packages/MaxMind.GeoIP2 |
| BCrypt.Net-Next | 4.0.3 | MIT | https://www.nuget.org/packages/BCrypt.Net-Next |
| MailKit | 4.16.0 | MIT | https://www.nuget.org/packages/MailKit |
| RazorLight | 2.3.1 | Apache-2.0 | https://www.nuget.org/packages/RazorLight |
| Stripe.net | 47.4.0 | Apache-2.0 | https://www.nuget.org/packages/Stripe.net |
| coverlet.collector | 6.0.4 | MIT | https://www.nuget.org/packages/coverlet.collector |
| Microsoft.AspNetCore.Mvc.Testing | 10.0.5 | MIT | https://www.nuget.org/packages/Microsoft.AspNetCore.Mvc.Testing |
| Microsoft.NET.Test.Sdk | 17.14.1 | MIT | https://www.nuget.org/packages/Microsoft.NET.Test.Sdk |
| Testcontainers.MsSql | 4.4.0 | MIT | https://www.nuget.org/packages/Testcontainers.MsSql |
| xunit | 2.9.3 | Apache-2.0 | https://www.nuget.org/packages/xunit |
| xunit.runner.visualstudio | 3.1.4 | MIT | https://www.nuget.org/packages/xunit.runner.visualstudio |

Transitive dependencies of the above packages are covered by their own licenses,
the majority of which are MIT or Apache-2.0. Run `dotnet list package --include-transitive`
for the full resolved graph.

## Bundled front-end libraries (`wwwroot/lib`)

| Library | License | Copyright | Home |
|---|---|---|---|
| Bootstrap 5.3.3 | MIT | © The Bootstrap Authors | https://getbootstrap.com |
| jQuery | MIT | © OpenJS Foundation and contributors | https://jquery.com |
| jQuery Validation | MIT | © Jörn Zaefferer | https://github.com/jquery-validation/jquery-validation |
| jQuery Validation Unobtrusive | Apache-2.0 | © Microsoft | https://github.com/aspnet/jquery-validation-unobtrusive |

## Editor (loaded at runtime via CDN)

| Component | License | Copyright | Home |
|---|---|---|---|
| Lexical | MIT | © Meta Platforms, Inc. and affiliates | https://lexical.dev |

## Data

| Dataset | License | Attribution |
|---|---|---|
| DB-IP IP-to-Country Lite database (`deploy/geoip/dbip-country-lite.mmdb`) | CC BY 4.0 | IP Geolocation by DB-IP — https://db-ip.com |

The DB-IP Lite database is distributed under the
[Creative Commons Attribution 4.0 International License](https://creativecommons.org/licenses/by/4.0/).
The attribution "IP Geolocation by DB-IP (https://db-ip.com)" is required wherever
the data is used and is retained in [deploy/geoip/README.md](deploy/geoip/README.md).
