#!/bin/bash

# First argument: target .NET major version (digit)
# Default to 8 if no argument is provided
DOTNET_VERSION="${1:-8}"

# Check .NET $DOTNET_VERSION
DOTNET_INFO=$(dotnet --info)
echo Checking for .NET $DOTNET_VERSION SDK in dotnet info output...
echo -------------------------------------------------------------

# Print matching lines
echo "$DOTNET_INFO" | grep -E "^\s*${DOTNET_VERSION}\.0\.[0-9]+\s+\[/usr/share/dotnet/sdk\]"

# Set environment variable based on match
if echo "$DOTNET_INFO" | grep -qE "^\s*${DOTNET_VERSION}\.0\.[0-9]+\s+\[/usr/share/dotnet/sdk\]"; then
  echo "DOTNET${DOTNET_VERSION}_installed=true" >> "$GITHUB_ENV"
else
  echo "DOTNET${DOTNET_VERSION}_installed=false" >> "$GITHUB_ENV"
fi
