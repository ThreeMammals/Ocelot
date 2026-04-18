#!/bin/bash
# Prepare Coveralls
echo "::group::Listing environment variables"
env | sort
echo "::endgroup::"

echo ------------ Detect coverage file ------------ 
coverage_1st_folder=$(ls -d /home/runner/work/Ocelot/Ocelot/artifacts/UnitTests/*/ | head -1)
echo "Detected first folder : $coverage_1st_folder"
coverage_file="${coverage_1st_folder%/}/coverage.cobertura.xml"
echo "Detecting file $coverage_file ..."
if [ -f "$coverage_file" ]; then
  echo "Coverage file exists."
  echo "COVERAGE_file_exists=true" >> $GITHUB_OUTPUT
  echo "COVERAGE_file=$coverage_file" >> $GITHUB_OUTPUT
else
  echo "Coverage file DOES NOT exist!"
  echo "COVERAGE_file_exists=false" >> $GITHUB_OUTPUT
fi
