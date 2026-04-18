#!/bin/bash

# First argument: target .NET major version (digit)
# Default to 8 if no argument is provided
DOTNET_VERSION="${1:-8}"

# Check .NET $DOTNET_VERSION
DOTNET_INFO=$(dotnet --info)
echo Checking for .NET $DOTNET_VERSION SDK in dotnet info output...
echo -------------------------------------------------------------

# Print matching lines
echo "$DOTNET_INFO" | grep -E "^\s*${DOTNET_VERSION}\.[0-9]+\.[0-9]+\s+\[.*[\/\\]sdk\]"

# Set environment variable based on match
if echo "$DOTNET_INFO" | grep -qE "^\s*${DOTNET_VERSION}\.[0-9]+\.[0-9]+\s+\[.*[\/\\]sdk\]"; then
  echo "checkdotnet_installed=true" >> $GITHUB_OUTPUT
else
  echo "checkdotnet_installed=false" >> $GITHUB_OUTPUT
fi
