#!/bin/bash
# Prepare Coveralls, with 1 optional argument
echo "::group::Listing environment variables"
env | sort
echo "::endgroup::"

# First argument: target .NET TFM (string)
# Default to "net10.0" if no argument is provided
DOTNET_TFM="${1:-net10.0}"

echo ------------ Detect coverage file ------------ 
ls -d ./test/Ocelot.UnitTests/bin/Debug/$DOTNET_TFM/*/
coverage_folder=$(ls -d ./test/Ocelot.UnitTests/bin/Debug/$DOTNET_TFM/TestResults*/ | head -1)
echo "Detected first folder : $coverage_folder"
echo TestResults files are...
ls $coverage_folder
echo DONE

coverage_pattern="${coverage_folder}/coverage.cobertura.*.xml"
# Expand the pattern to an array
coverage_files=($coverage_pattern)
coverage_file="${coverage_files[0]}"

echo "Detecting file $coverage_file ..."
if [ -f "$coverage_file" ]; then
  echo "Coverage file exists."
  echo "COVERAGE_file_exists=true" >> $GITHUB_OUTPUT
  echo "COVERAGE_file=$coverage_file" >> $GITHUB_OUTPUT
else
  echo "Coverage file DOES NOT exist!"
  echo "COVERAGE_file_exists=false" >> $GITHUB_OUTPUT
fi
