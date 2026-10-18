#!/bin/bash
set -x

# Ensure we're in the correct directory
cd /var/app/current

# List all files to verify runtimeconfig exists  
echo "Files in /var/app/current:"
ls -la

# Check for runtimeconfig files
echo "Looking for .runtimeconfig.json files:"
find . -name "*.runtimeconfig.json" -type f

# Grant execute permissions if needed
chmod +x *.exe 2>/dev/null || true
