param(
    [string]$Target = "user@app-server",
    [string]$RemoteDir = "~/howtoosoftware",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# Resolve paths from the repository root regardless of the invocation directory
# (this script lives in scripts/; its parent directory is the repo root).
Set-Location (Split-Path -Parent $PSScriptRoot)

Write-Host "=== Deploying HowToSoftware to $Target ===" -ForegroundColor Cyan

# Step 1: Create tar archive
Write-Host "`n[1/4] Packaging deployment files..." -ForegroundColor Yellow

$tarFile = "deploy-bundle.tar.gz"

$includes = @(
    "docker-compose.yml",
    "docker-compose.prod.yml",
    ".dockerignore",
    "Directory.Build.props",
    "Directory.Packages.props",
    "deploy",
    "src/HowToSoftware.Core",
    "src/HowToSoftware.Infrastructure",
    "src/HowToSoftware.Web",
    "src/HowToSoftware.Admin"
)

$tarArgs = @("-czf", $tarFile, "--exclude=*/bin", "--exclude=*/obj", "--exclude=*/.vs") + $includes
& tar @tarArgs
if ($LASTEXITCODE -ne 0) { throw "tar failed" }

$size = [math]::Round((Get-Item $tarFile).Length / 1MB, 1)
Write-Host "  Archive: $tarFile ($size MB)" -ForegroundColor DarkGray

# Step 2: Upload and extract on remote
Write-Host "[2/4] Uploading to ${Target}:${RemoteDir} ..." -ForegroundColor Yellow

ssh $Target "mkdir -p $RemoteDir"

scp $tarFile "${Target}:${RemoteDir}/$tarFile"
if ($LASTEXITCODE -ne 0) { throw "scp failed" }

$extractCmd = "cd $RemoteDir ; rm -rf src deploy ; tar -xzf $tarFile ; rm $tarFile"
ssh $Target $extractCmd
if ($LASTEXITCODE -ne 0) { throw "remote extract failed" }

Remove-Item $tarFile -ErrorAction SilentlyContinue

# Step 3: Ensure .env exists
Write-Host "[3/4] Checking .env on remote..." -ForegroundColor Yellow

$checkCmd = "test -f $RemoteDir/.env ; echo `$?"
$envResult = ssh $Target $checkCmd
if ($envResult.Trim() -ne "0") {
    Write-Host "  No .env file found. Creating one interactively." -ForegroundColor Yellow

    $dbPass = Read-Host -Prompt "  DB_PASSWORD (SQL Server password for User_WebsiteHowTooSoftware)"
    if ([string]::IsNullOrWhiteSpace($dbPass)) { throw "DB_PASSWORD is required" }

    $dbHost = Read-Host -Prompt "  DB_HOST (SQL Server host:port) [sql-host,1433]"
    if ([string]::IsNullOrWhiteSpace($dbHost)) { $dbHost = "sql-host,1433" }

    $siteUrl = Read-Host -Prompt "  SITE_URL [howtoosoftware.com]"
    if ([string]::IsNullOrWhiteSpace($siteUrl)) { $siteUrl = "howtoosoftware.com" }

    $mgDomain = Read-Host -Prompt "  MAILGUN_DOMAIN [mg.howtoosoftware.com]"
    if ([string]::IsNullOrWhiteSpace($mgDomain)) { $mgDomain = "mg.howtoosoftware.com" }

    $mgKey = Read-Host -Prompt "  MAILGUN_API_KEY (leave blank to skip)"

    $lines = @(
        "DB_PASSWORD=$dbPass",
        "DB_HOST=$dbHost",
        "SITE_URL=$siteUrl",
        "MAILGUN_DOMAIN=$mgDomain"
    )
    if (-not [string]::IsNullOrWhiteSpace($mgKey)) {
        $lines += "MAILGUN_API_KEY=$mgKey"
    }

    # Write each line via separate echo commands
    $first = $true
    foreach ($line in $lines) {
        if ($first) {
            $writeCmd = "echo '$line' > $RemoteDir/.env"
            $first = $false
        } else {
            $writeCmd = "echo '$line' >> $RemoteDir/.env"
        }
        ssh $Target $writeCmd
    }
    if ($LASTEXITCODE -ne 0) { throw "Failed to create .env on remote" }
    Write-Host "  .env created on remote." -ForegroundColor Green
} else {
    Write-Host "  .env exists." -ForegroundColor DarkGray
}

# Step 4: Build and start
if (-not $SkipBuild) {
    Write-Host "[4/4] Building and starting containers..." -ForegroundColor Yellow
    $upCmd = "cd $RemoteDir ; docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --build --remove-orphans"
} else {
    Write-Host "[4/4] Restarting containers (skip build)..." -ForegroundColor Yellow
    $upCmd = "cd $RemoteDir ; docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d --remove-orphans"
}
ssh $Target $upCmd

Write-Host "`n=== Deployment complete ===" -ForegroundColor Green
Write-Host "Check status: ssh $Target 'cd $RemoteDir ; docker compose ps'"
Write-Host "View logs:    ssh $Target 'cd $RemoteDir ; docker compose logs -f'"

# =============================================================
# © 2026 Henry Lawrence Cahill (HowToSoftware). All rights reserved.
# Contact: henry.cahill@howtoosoftware.com | https://howtoosoftware.com
# =============================================================
