#!/bin/bash

# Publish script for Hamad.Api to hamad.runasp.net
# Based on WebDeploy settings

echo "Starting publish process..."

# Configuration
PROJECT_PATH="src/BenhaScooters/BenhaScooters.csproj"
CONFIGURATION="Release"
OUTPUT_DIR="./publish-output"

# FTP/SFTP settings (read from environment or prompt)
FTP_HOST="${FTP_HOST:-site53013.siteasp.net}"
FTP_PORT="${FTP_PORT:-21}"  # Use 21 for FTP, 22 for SFTP
FTP_USER="${FTP_USER:-}"
FTP_PASS="${FTP_PASS:-}"
FTP_REMOTE_DIR="${FTP_REMOTE_DIR:-/wwwroot}"  # Website root directory
DESTINATION_URL="${DESTINATION_URL:-http://banha-riders.runasp.net/}"

# Clean previous publish output
if [ -d "$OUTPUT_DIR" ]; then
    echo "Cleaning previous publish output..."
    rm -rf "$OUTPUT_DIR"
fi

# Publish the application
echo "Publishing application..."
dotnet publish "$PROJECT_PATH" \
    --configuration "$CONFIGURATION" \
    --output "$OUTPUT_DIR" \
    /p:PublishProfile=Release

if [ $? -ne 0 ]; then
    echo "Error: Publish failed!"
    exit 1
fi

echo "Build completed successfully!"
echo "Output directory: $OUTPUT_DIR"

# Add a small delay to ensure all files are written
sleep 2

# Deploy via FTP
read -p "Do you want to upload to $DESTINATION_URL via FTP? (y/n): " -n 1 -r
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo ""
    echo "Upload options:"
    echo "1. Sync only new/modified files (may miss some DLLs)"
    echo "2. Sync and delete removed files (full mirror)"
    echo "3. Force upload ALL files (recommended for DLL issues)"
    read -p "Choose option (1, 2, or 3) [default: 3]: " -n 1 -r SYNC_OPTION
    echo ""
    
    if [ -z "$SYNC_OPTION" ]; then
        SYNC_OPTION="3"
    fi
    
    # Check if lftp is installed
    if ! command -v lftp &> /dev/null; then
        echo "Error: 'lftp' is not installed."
        echo "Install it with: sudo apt install lftp (Ubuntu/Debian) or sudo pacman -S lftp (Arch)"
        echo ""
        echo "Alternatively, you can use SFTP manually:"
        echo "  sftp $FTP_USER@$FTP_HOST"
        echo "  cd $FTP_REMOTE_DIR"
        echo "  put -r $OUTPUT_DIR/*"
        exit 1
    fi
    
    # Set mirror options based on user choice
    case $SYNC_OPTION in
        2)
            MIRROR_OPTIONS="--reverse --delete --verbose --parallel=3"
            echo "Syncing with delete (full mirror)..."
            ;;
        3)
            MIRROR_OPTIONS="--reverse --verbose --parallel=3"
            echo "Force uploading ALL files (ignoring timestamps)..."
            ;;
        *)
            MIRROR_OPTIONS="--reverse --verbose --only-newer --parallel=3"
            echo "Syncing only new/modified files..."
            ;;
    esac
    
    # Prompt for credentials if not supplied via env
    if [ -z "$FTP_USER" ]; then
        read -p "FTP Username: " FTP_USER
    fi
    if [ -z "$FTP_PASS" ]; then
        read -s -p "FTP Password: " FTP_PASS
        echo ""
    fi

    # Upload using lftp
    echo "Connecting to $FTP_HOST:$FTP_PORT..."
    
    # Increase parallelism and performance
    lftp -c "
    set ftp:ssl-allow no
    set ftp:passive-mode on
    set ftp:use-site-chmod no
    set mirror:set-permissions no
    set net:max-retries 3
    set net:reconnect-interval-base 2
    set net:timeout 20
    set net:socket-buffer 65536
    set ftp:timezone \"\"
    open -u $FTP_USER,$FTP_PASS -p $FTP_PORT $FTP_HOST
    lcd $OUTPUT_DIR
    cd $FTP_REMOTE_DIR
    mirror $MIRROR_OPTIONS --parallel=10 
    bye
    "
    
    if [ $? -eq 0 ]; then
        echo ""
        echo "✓ Deployment completed successfully!"
        
        # Verify uploaded files
        echo ""
        echo "Verifying uploaded files..."
        lftp -c "
        set ftp:ssl-allow no
        open -u $FTP_USER,$FTP_PASS -p $FTP_PORT $FTP_HOST
        cd $FTP_REMOTE_DIR
        cls -1 *.dll
        bye
        " > /tmp/uploaded_dlls.txt
        
        if [ -s /tmp/uploaded_dlls.txt ]; then
            echo "✓ DLL files on server:"
            cat /tmp/uploaded_dlls.txt
            rm /tmp/uploaded_dlls.txt
        fi
        
        echo ""
        echo "✓ Site URL: $DESTINATION_URL"
    else
        echo ""
        echo "✗ Deployment failed. Please check your FTP credentials and connection."
        echo ""
        echo "Manual deployment options:"
        echo "1. FTP: ftp $FTP_HOST (user: $FTP_USER)"
        echo "2. SFTP: sftp $FTP_USER@$FTP_HOST"
        echo "3. Upload files from: $OUTPUT_DIR"
        exit 1
    fi
else
    echo "Skipping upload. Files are ready in: $OUTPUT_DIR"
    echo ""
    echo "To deploy manually:"
    echo "  Host: $FTP_HOST"
    echo "  User: $FTP_USER"
    echo "  Password: $FTP_PASS"
    echo "  Remote directory: $FTP_REMOTE_DIR"
fi