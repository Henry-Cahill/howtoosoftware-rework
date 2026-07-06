# deploy-theme.ps1 - Deploy Ghost theme and compose file to Docker
# Usage: .\deploy-theme.ps1
# Or: .\deploy-theme.ps1 -SkipCompose  (to only deploy theme)
# Or: .\deploy-theme.ps1 -ComposeOnly  (to only deploy compose file)

param(
    [string]$ThemeName = "howtoosoftware-custom",
    [string]$Server = "user@theme-server",
    [string]$ThemePath = $PSScriptRoot,
    [string]$VolumeBase = "/var/lib/docker/volumes/hts_theme_source/_data",
    [string]$ComposeDir = "~/hts",
    [string]$ComposeFile = "hts_compose_local.yml",
    [switch]$SkipCompose,
    [switch]$ComposeOnly,
    [switch]$SkipRestart
)

$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host "  Ghost Theme and Compose Deployer" -ForegroundColor Cyan
Write-Host "==============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Theme:  $ThemeName" -ForegroundColor Yellow
Write-Host "  Server: $Server" -ForegroundColor Yellow
Write-Host "  Source: $ThemePath" -ForegroundColor Yellow
Write-Host ""

$StepNum = 1
$TotalSteps = 5
if ($SkipCompose) { $TotalSteps = 4 }
if ($ComposeOnly) { $TotalSteps = 2 }

# ============================================
# Step: Upload Compose File (if not skipped)
# ============================================
if (-not $SkipCompose) {
    Write-Host "[$StepNum/$TotalSteps] Uploading compose file..." -ForegroundColor Green
    $ComposeLocalPath = Join-Path $ThemePath $ComposeFile
    if (Test-Path $ComposeLocalPath) {
        scp $ComposeLocalPath "${Server}:${ComposeDir}/${ComposeFile}"
        if ($LASTEXITCODE -ne 0) { throw "Failed to upload compose file" }
        Write-Host "  Done - Compose file uploaded" -ForegroundColor Gray
    } else {
        Write-Host "  Warning - Compose file not found: $ComposeLocalPath" -ForegroundColor Yellow
    }
    $StepNum++
}

if ($ComposeOnly) {
    # Just restart with new compose
    Write-Host "[$StepNum/$TotalSteps] Restarting stack with new compose..." -ForegroundColor Green
    $restartCmd = "cd $ComposeDir; docker compose -f $ComposeFile down; docker compose -f $ComposeFile up -d"
    ssh -t $Server $restartCmd
    
    Write-Host ""
    Write-Host "==============================================================" -ForegroundColor Green
    Write-Host "  Done - Compose deployment complete!" -ForegroundColor Green
    Write-Host "==============================================================" -ForegroundColor Green
    Write-Host ""
    exit 0
}

# ============================================
# Step: Create theme archive
# ============================================
Write-Host "[$StepNum/$TotalSteps] Creating theme archive..." -ForegroundColor Green
$TarFile = "$env:TEMP\$ThemeName.tar"
$ExcludeFiles = @(
    "node_modules",
    "dist",
    ".git",
    "*.tar",
    "*.zip",
    "deploy-theme.ps1",
    "deploy-theme.sh",
    ".vscode"
)

$ExcludeArgs = $ExcludeFiles | ForEach-Object { "--exclude=$_" }

Push-Location $ThemePath
try {
    $tarCmd = "tar -cf `"$TarFile`" $($ExcludeArgs -join ' ') ."
    Invoke-Expression $tarCmd
    if ($LASTEXITCODE -ne 0) { throw "Failed to create tar archive" }
    $TarSize = [math]::Round((Get-Item $TarFile).Length / 1KB, 1)
    Write-Host "  Done - Archive created ($TarSize KB)" -ForegroundColor Gray
}
finally {
    Pop-Location
}
$StepNum++

# ============================================
# Step: Upload tar to server
# ============================================
Write-Host "[$StepNum/$TotalSteps] Uploading theme to server..." -ForegroundColor Green
scp $TarFile "${Server}:/tmp/${ThemeName}.tar"
if ($LASTEXITCODE -ne 0) { throw "Failed to upload theme" }
Write-Host "  Done - Upload complete" -ForegroundColor Gray
$StepNum++

# ============================================
# Step: Extract to Docker volume (using docker to avoid sudo)
# ============================================
Write-Host "[$StepNum/$TotalSteps] Extracting to Docker volume..." -ForegroundColor Green
# Use a temporary alpine container to extract the tar into the volume
# This avoids needing sudo since we're in the docker group
$extractCmd = "docker run --rm -v hts_theme_source:/theme -v /tmp:/tmp alpine sh -c 'rm -rf /theme/* && tar -xf /tmp/$ThemeName.tar -C /theme/ && chown -R 1000:1000 /theme/'; rm /tmp/$ThemeName.tar"
ssh $Server $extractCmd
if ($LASTEXITCODE -ne 0) { throw "Failed to extract theme" }
Write-Host "  Done - Theme extracted to volume" -ForegroundColor Gray
$StepNum++

# ============================================
# Step: Restart stack
# ============================================
if (-not $SkipRestart) {
    Write-Host "[$StepNum/$TotalSteps] Restarting Docker stack..." -ForegroundColor Green
    Write-Host "  This will take 1-2 minutes..." -ForegroundColor Gray
    $restartCmd = "cd $ComposeDir; docker compose -f $ComposeFile down; docker compose -f $ComposeFile up -d"
    ssh -t $Server $restartCmd
    if ($LASTEXITCODE -ne 0) { throw "Failed to restart stack" }
    Write-Host "  Done - Stack restarted" -ForegroundColor Gray
} else {
    Write-Host "[$StepNum/$TotalSteps] Skipping restart (manual restart required)" -ForegroundColor Yellow
}

# Cleanup local tar
Remove-Item $TarFile -ErrorAction SilentlyContinue

Write-Host ""
Write-Host "==============================================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "==============================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  Theme '$ThemeName' deployed successfully!" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Wait ~60 seconds for Ghost to fully start, then test:" -ForegroundColor Yellow
Write-Host "    https://howtoosoftware.com" -ForegroundColor White
Write-Host ""
Write-Host "  Monitor logs:" -ForegroundColor Yellow
Write-Host "    ssh $Server docker logs -f hts-ghost" -ForegroundColor Gray
Write-Host "    ssh $Server docker logs hts-theme-builder" -ForegroundColor Gray
Write-Host ""
