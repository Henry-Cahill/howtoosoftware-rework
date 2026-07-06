# GeoIP database

`dbip-country-lite.mmdb` is the [DB-IP IP-to-Country Lite](https://db-ip.com/db/download/ip-to-country-lite)
database. It is updated monthly and is licensed under CC BY 4.0
(attribution: "IP Geolocation by DB-IP"). It uses the MaxMind MMDB format,
so it is read by `MaxMind.GeoIP2`'s `DatabaseReader`.

The web container loads it via `GeoIp__DatabasePath` (see
`docker-compose.yml`) and is consumed by `GeoIpService` in
`HowToSoftware.Infrastructure`.

## Refreshing

Run from the repo root:

```powershell
$y = (Get-Date).Year; $m = '{0:D2}' -f (Get-Date).Month
Invoke-WebRequest -Uri "https://download.db-ip.com/free/dbip-country-lite-$y-$m.mmdb.gz" `
    -OutFile deploy\geoip\dbip-country-lite.mmdb.gz
$in = [System.IO.File]::OpenRead((Resolve-Path deploy\geoip\dbip-country-lite.mmdb.gz))
$gs = New-Object System.IO.Compression.GZipStream($in,[System.IO.Compression.CompressionMode]::Decompress)
$os = [System.IO.File]::Create((Join-Path (Resolve-Path deploy\geoip) "dbip-country-lite.mmdb"))
$gs.CopyTo($os); $os.Close(); $gs.Close(); $in.Close()
Remove-Item deploy\geoip\dbip-country-lite.mmdb.gz
```

Then redeploy.
