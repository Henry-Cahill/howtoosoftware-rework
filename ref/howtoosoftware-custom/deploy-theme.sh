#!/bin/bash
# deploy-theme.sh - Deploy Ghost theme and compose file to Docker
# Usage: ./deploy-theme.sh
# Or: ./deploy-theme.sh --skip-compose    (to only deploy theme)
# Or: ./deploy-theme.sh --compose-only    (to only deploy compose file)
# Or: ./deploy-theme.sh --skip-restart    (don't restart stack)

set -e

# Defaults
THEME_NAME="howtoosoftware-custom"
SERVER="user@theme-server"
THEME_PATH="."
VOLUME_BASE="/var/lib/docker/volumes/hts_theme_source/_data"
COMPOSE_DIR="~/hts"
COMPOSE_FILE="hts_compose_local.yml"
SKIP_COMPOSE=false
COMPOSE_ONLY=false
SKIP_RESTART=false

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --skip-compose) SKIP_COMPOSE=true; shift ;;
        --compose-only) COMPOSE_ONLY=true; shift ;;
        --skip-restart) SKIP_RESTART=true; shift ;;
        --server) SERVER="$2"; shift 2 ;;
        --theme) THEME_NAME="$2"; shift 2 ;;
        *) THEME_PATH="$1"; shift ;;
    esac
done

echo ""
echo "══════════════════════════════════════════════════════════════"
echo "  Ghost Theme & Compose Deployer"
echo "══════════════════════════════════════════════════════════════"
echo ""
echo "  Theme:  $THEME_NAME"
echo "  Server: $SERVER"
echo "  Source: $THEME_PATH"
echo ""

STEP=1
if [ "$SKIP_COMPOSE" = true ]; then
    TOTAL=4
elif [ "$COMPOSE_ONLY" = true ]; then
    TOTAL=2
else
    TOTAL=5
fi

# ============================================
# Step: Upload Compose File (if not skipped)
# ============================================
if [ "$SKIP_COMPOSE" = false ]; then
    echo "[$STEP/$TOTAL] Uploading compose file..."
    COMPOSE_LOCAL="$THEME_PATH/$COMPOSE_FILE"
    if [ -f "$COMPOSE_LOCAL" ]; then
        scp "$COMPOSE_LOCAL" "${SERVER}:${COMPOSE_DIR}/${COMPOSE_FILE}"
        echo "  ✓ Compose file uploaded"
    else
        echo "  ⚠ Compose file not found: $COMPOSE_LOCAL"
    fi
    STEP=$((STEP + 1))
fi

if [ "$COMPOSE_ONLY" = true ]; then
    echo "[$STEP/$TOTAL] Restarting stack with new compose..."
    ssh -t "$SERVER" "cd $COMPOSE_DIR && docker compose -f $COMPOSE_FILE down && docker compose -f $COMPOSE_FILE up -d"
    
    echo ""
    echo "══════════════════════════════════════════════════════════════"
    echo "  ✓ Compose deployment complete!"
    echo "══════════════════════════════════════════════════════════════"
    echo ""
    exit 0
fi

# ============================================
# Step: Create theme archive
# ============================================
echo "[$STEP/$TOTAL] Creating theme archive..."
TAR_FILE="/tmp/${THEME_NAME}.tar"
tar -cf "$TAR_FILE" \
    --exclude='node_modules' \
    --exclude='dist' \
    --exclude='.git' \
    --exclude='*.tar' \
    --exclude='*.zip' \
    --exclude='deploy-theme.ps1' \
    --exclude='deploy-theme.sh' \
    --exclude='.vscode' \
    -C "$THEME_PATH" .
TAR_SIZE=$(du -h "$TAR_FILE" | cut -f1)
echo "  ✓ Archive created ($TAR_SIZE)"
STEP=$((STEP + 1))

# ============================================
# Step: Upload to server
# ============================================
echo "[$STEP/$TOTAL] Uploading theme to server..."
scp "$TAR_FILE" "${SERVER}:/tmp/${THEME_NAME}.tar"
echo "  ✓ Upload complete"
STEP=$((STEP + 1))

# ============================================
# Step: Extract to Docker volume
# ============================================
echo "[$STEP/$TOTAL] Extracting to Docker volume..."
ssh "$SERVER" "sudo rm -rf $VOLUME_BASE/* && sudo tar -xf /tmp/${THEME_NAME}.tar -C $VOLUME_BASE/ && sudo chown -R 1000:1000 $VOLUME_BASE/ && rm /tmp/${THEME_NAME}.tar"
echo "  ✓ Theme extracted to volume"
STEP=$((STEP + 1))

# ============================================
# Step: Restart stack
# ============================================
if [ "$SKIP_RESTART" = false ]; then
    echo "[$STEP/$TOTAL] Restarting Docker stack..."
    echo "  This will take 1-2 minutes..."
    ssh -t "$SERVER" "cd $COMPOSE_DIR && docker compose -f $COMPOSE_FILE down && docker compose -f $COMPOSE_FILE up -d"
    echo "  ✓ Stack restarted"
else
    echo "[$STEP/$TOTAL] Skipping restart (manual restart required)"
fi

# Cleanup
rm -f "$TAR_FILE"

echo ""
echo "══════════════════════════════════════════════════════════════"
echo "  ✓ Deployment Complete!"
echo "══════════════════════════════════════════════════════════════"
echo ""
echo "  Theme '$THEME_NAME' deployed successfully!"
echo ""
echo "  Wait ~60 seconds for Ghost to fully start, then test:"
echo "    https://howtoosoftware.com"
echo ""
echo "  Monitor logs:"
echo "    ssh $SERVER \"docker logs -f hts-ghost\""
echo "    ssh $SERVER \"docker logs hts-theme-builder\""
echo ""
