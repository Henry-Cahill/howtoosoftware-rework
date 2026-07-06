<#
.SYNOPSIS
  Interactively create Cloudflare DNS records for howtoosoftware.com.
  Each record is previewed and you approve [y/N] before it is created.

.USAGE
  pwsh -File .\Apply-CloudflareRecords.ps1
  (or in Windows PowerShell 5.1: powershell -File .\Apply-CloudflareRecords.ps1)

.NOTES
  Needs a Cloudflare API token with permission:
      Zone -> DNS -> Edit   on howtoosoftware.com
  Create one at: https://dash.cloudflare.com/profile/api-tokens
#>

[CmdletBinding()]
param(
    [string]$Domain = "howtoosoftware.com",
    [string]$OriginIPv4 = "136.34.124.155",
    [string]$OriginIPv6 = "2605:a600:1e90:bf8::1",
    [string]$DmarcEmail = "henry.cahill@howtoosoftware.com",
    [string]$DkimSelector = "mx",
    [string]$DkimValue = ""   # paste the "k=rsa; p=..." string here later, or leave empty to skip
)

$ErrorActionPreference = "Stop"

# --- 1. Auth -----------------------------------------------------------------
Write-Host ""
Write-Host "Cloudflare DNS bootstrap for $Domain" -ForegroundColor Cyan
Write-Host "------------------------------------------------------------"
$secureToken = Read-Host "Paste your Cloudflare API token" -AsSecureString
$bstr        = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secureToken)
$token       = [Runtime.InteropServices.Marshal]::PtrToStringAuto($bstr)
[Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr) | Out-Null

$headers = @{
    "Authorization" = "Bearer $token"
    "Content-Type"  = "application/json"
}
$baseUri = "https://api.cloudflare.com/client/v4"

# --- 2. Resolve zone id ------------------------------------------------------
Write-Host "Looking up zone id for $Domain ..." -ForegroundColor DarkGray
$zoneResp = Invoke-RestMethod -Method GET -Headers $headers `
    -Uri "$baseUri/zones?name=$Domain"
if (-not $zoneResp.success -or $zoneResp.result.Count -eq 0) {
    throw "Could not find zone $Domain. Check the token has access to this zone."
}
$zoneId = $zoneResp.result[0].id
Write-Host "Zone id: $zoneId" -ForegroundColor Green

# --- 3. Pull existing records so we can warn on duplicates -------------------
$existing = (Invoke-RestMethod -Method GET -Headers $headers `
    -Uri "$baseUri/zones/$zoneId/dns_records?per_page=200").result

function Find-Existing($type, $name) {
    $fqdn = if ($name -eq '@') { $Domain } else { "$name.$Domain" }
    return $existing | Where-Object { $_.type -eq $type -and $_.name -eq $fqdn }
}

# --- 4. Build the desired record list ----------------------------------------
$records = @(
    @{ type='A';     name='@';   content=$OriginIPv4;            proxied=$true;  ttl=1 },
    @{ type='AAAA';  name='@';   content=$OriginIPv6;            proxied=$true;  ttl=1 },
    @{ type='CNAME'; name='www'; content=$Domain;                proxied=$true;  ttl=1 },

    # Mailgun (US region) -- send + receive
    @{ type='TXT';   name='@';   content='v=spf1 include:mailgun.org ~all'; proxied=$false; ttl=1 },
    @{ type='MX';    name='@';   content='mxa.mailgun.org';     proxied=$false; ttl=1; priority=10 },
    @{ type='MX';    name='@';   content='mxb.mailgun.org';     proxied=$false; ttl=1; priority=10 },

    # DMARC -- monitor mode to start
    @{ type='TXT';   name='_dmarc'; content=("v=DMARC1; p=none; rua=mailto:{0}; fo=1; adkim=r; aspf=r;" -f $DmarcEmail);
       proxied=$false; ttl=1 },

    # CAA -- only these CAs may issue certs for the domain
    @{ type='CAA';   name='@'; data=@{ flags=0; tag='issue';     value='letsencrypt.org' };               ttl=1 },
    @{ type='CAA';   name='@'; data=@{ flags=0; tag='issue';     value='pki.goog' };                      ttl=1 },
    @{ type='CAA';   name='@'; data=@{ flags=0; tag='issue';     value='ssl.com' };                       ttl=1 },
    @{ type='CAA';   name='@'; data=@{ flags=0; tag='issue';     value='digicert.com' };                  ttl=1 },
    @{ type='CAA';   name='@'; data=@{ flags=0; tag='issuewild'; value='letsencrypt.org' };               ttl=1 },
    @{ type='CAA';   name='@'; data=@{ flags=0; tag='iodef';     value=("mailto:{0}" -f $DmarcEmail) };   ttl=1 }
)

# Optional DKIM
if ($DkimValue -and $DkimValue -ne 'later') {
    $records += @{ type='TXT'; name="$DkimSelector._domainkey"; content=$DkimValue; proxied=$false; ttl=1 }
} else {
    Write-Host ""
    Write-Host "NOTE: DKIM TXT skipped (no value supplied). Add it manually from Mailgun later." -ForegroundColor Yellow
}

# --- 5. Walk records, prompt y/N, POST each one ------------------------------
function Format-Record($r) {
    $fqdn = if ($r.name -eq '@') { $Domain } else { "$($r.name).$Domain" }
    switch ($r.type) {
        'MX'  { "MX  $fqdn  pri=$($r.priority)  $($r.content)" }
        'CAA' { "CAA $fqdn  $($r.data.flags) $($r.data.tag) `"$($r.data.value)`"" }
        default {
            $proxy = if ($r.proxied) { ' [proxied]' } else { ' [dns-only]' }
            "$($r.type)  $fqdn  =>  $($r.content)$proxy"
        }
    }
}

$created = 0; $skipped = 0; $failed = 0
foreach ($r in $records) {
    Write-Host ""
    Write-Host "----------------------------------------------------" -ForegroundColor DarkGray
    Write-Host (Format-Record $r) -ForegroundColor White

    $dupes = Find-Existing $r.type $r.name
    if ($dupes) {
        Write-Host "WARNING: $($dupes.Count) existing $($r.type) record(s) with this name already exist:" -ForegroundColor Yellow
        $dupes | ForEach-Object { Write-Host "    - $($_.content)" -ForegroundColor Yellow }
    }

    $ans = Read-Host "Create this record? [y/N]"
    if ($ans -notmatch '^(y|yes)$') {
        Write-Host "skipped." -ForegroundColor DarkGray
        $skipped++
        continue
    }

    # Build request body
    $body = @{ type = $r.type; name = $r.name; ttl = $r.ttl }
    if ($r.type -eq 'CAA') {
        $body.data = $r.data
    } else {
        $body.content = $r.content
        if ($r.ContainsKey('proxied'))  { $body.proxied  = $r.proxied }
        if ($r.ContainsKey('priority')) { $body.priority = $r.priority }
    }

    try {
        $resp = Invoke-RestMethod -Method POST -Headers $headers `
            -Uri "$baseUri/zones/$zoneId/dns_records" `
            -Body ($body | ConvertTo-Json -Depth 5)
        if ($resp.success) {
            Write-Host "created (id $($resp.result.id))" -ForegroundColor Green
            $created++
        } else {
            Write-Host "FAILED: $($resp.errors | ConvertTo-Json -Depth 5)" -ForegroundColor Red
            $failed++
        }
    } catch {
        Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message -ForegroundColor Red }
        $failed++
    }
}

Write-Host ""
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ("Done.  created={0}  skipped={1}  failed={2}" -f $created, $skipped, $failed) -ForegroundColor Cyan
Write-Host ""
Write-Host "Next manual steps (not DNS records):" -ForegroundColor Cyan
Write-Host "  1. Cloudflare -> Rules -> Redirect Rules: www -> apex (301)."
Write-Host "  2. Cloudflare -> SSL/TLS -> Overview: set Full (Strict)."
Write-Host "  3. Cloudflare -> SSL/TLS -> Origin Server: issue an origin cert,"
Write-Host "     install it on $OriginIPv4."
Write-Host "  4. Cloudflare -> DNS -> Settings: enable DNSSEC, copy DS to registrar."
Write-Host "  5. Add Mailgun DKIM TXT once you copy the value from Mailgun."
