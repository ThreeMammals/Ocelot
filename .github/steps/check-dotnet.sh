#!/bin/bash

# First argument: target .NET major version (digit)
# Default to 8 if no argument is provided
DOTNET_VERSION="${1:-8}"

# # Check .NET $DOTNET_VERSION
# DOTNET_INFO=$(dotnet --info)
# echo Checking for .NET $DOTNET_VERSION SDK in dotnet info output...
# echo -------------------------------------------------------------

# # Print matching lines
# echo "$DOTNET_INFO" | grep -E "^\s*${DOTNET_VERSION}\.[0-9]+\.[0-9]+\s+\[.*[\/\\]sdk\]\s*$"

# # Set environment variable based on match
# if echo "$DOTNET_INFO" | grep -qE "^\s*${DOTNET_VERSION}\.[0-9]+\.[0-9]+\s+\[.*[\/\\]sdk\]\s*$"; then
#   echo "checkdotnet_installed=true" >> $GITHUB_OUTPUT
# else
#   echo "checkdotnet_installed=false" >> $GITHUB_OUTPUT
# fi

DOTNET_INFO=$(dotnet --info)

echo "Checking for .NET $DOTNET_VERSION SDK..."
echo "-------------------------------------------------------------"

# Split DOTNET_INFO into lines and iterate
mapfile -t lines <<< "$DOTNET_INFO"

found=false

for line in "${lines[@]}"; do
    # Skip empty lines and non-SDK lines
    if [[ $line =~ ^[[:space:]]*${DOTNET_VERSION}\.[0-9]+\.[0-9]+[[:space:]]+\[.*[\/\\]sdk\] ]]; then
        echo "$line"
        found=true
    fi
done

echo "-------------------------------------------------------------"

if [[ $found == false ]]; then
    echo "checkdotnet_installed=false" >> $GITHUB_OUTPUT
    echo "❌ No .NET ${DOTNET_VERSION} SDK found!"
    exit 1
else
    echo "checkdotnet_installed=true" >> $GITHUB_OUTPUT
    echo "✅ Found .NET ${DOTNET_VERSION} SDK(s) above."
fi
