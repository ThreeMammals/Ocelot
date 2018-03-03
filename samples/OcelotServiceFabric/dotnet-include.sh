#!/bin/bash

. /etc/os-release
linuxDistrib=$ID
if [ $linuxDistrib = "rhel" ]; then
  source scl_source enable rh-dotnet20
  exitCode=$?
  if [ $exitCode != 0 ]; then
    echo "Failed: source scl_source enable rh-dotnet20 : ExitCode: $exitCode"
    exit $exitCode
  fi
fi
