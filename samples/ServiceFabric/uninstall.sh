#!/bin/bash -x

sfctl application delete --application-id OcelotServiceApplication
sfctl application unprovision --application-type-name OcelotServiceApplicationType --application-type-version 1.0.0
sfctl store delete --content-path OcelotServiceApplication