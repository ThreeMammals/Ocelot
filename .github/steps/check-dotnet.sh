#!/bin/bash

# First argument: target .NET major version (digit)
# Default to 8 if no argument is provided
# Target major version (e.g. 10, 9, 8...)
DOTNET_VERSION="${1:-8}"

echo "All SDKs..."
echo "-------------------------------------------------------------"
dotnet --list-sdks
echo "-------------------------------------------------------------"

echo "Checking for .NET ${DOTNET_VERSION} SDK..."

# Use dotnet --list-sdks and check if any SDK starts with the major version
if dotnet --list-sdks | grep -q -E "^\s*${DOTNET_VERSION}\."; then
    echo "CHECKDOTNET_installed=true" >> $GITHUB_OUTPUT
    echo "✅ .NET ${DOTNET_VERSION} SDK is installed."    
    # Optional: Show the actual installed versions
    echo "Installed ${DOTNET_VERSION}.x SDKs:"
    dotnet --list-sdks | grep -E "^\s*${DOTNET_VERSION}\."
else
    echo "CHECKDOTNET_installed=false" >> $GITHUB_OUTPUT
    echo "❌ .NET ${DOTNET_VERSION} SDK is NOT installed."
fi
