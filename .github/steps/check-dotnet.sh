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

found=false

# Iterate line by line from the variable (works on Ubuntu)
while IFS= read -r line || [[ -n "$line" ]]; do
    # Match lines like: "  10.0.202 [C:\Program Files\dotnet\sdk]"
    if [[ $line =~ ^[[:space:]]*${DOTNET_VERSION}\.[0-9]+\.[0-9]+[[:space:]]+\[.*[\/\\]sdk\] ]]; then
        echo "$line"
        found=true
    fi
done <<< "$DOTNET_INFO"

echo "-------------------------------------------------------------"

if [[ $found == false ]]; then
    echo "checkdotnet_installed=false" >> $GITHUB_OUTPUT
    echo "❌ No .NET ${DOTNET_VERSION} SDK found!"
else
    echo "checkdotnet_installed=true" >> $GITHUB_OUTPUT
    echo "✅ Found .NET ${DOTNET_VERSION} SDK(s) above."
fi
