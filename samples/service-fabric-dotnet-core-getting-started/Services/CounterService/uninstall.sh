#!/bin/bash -x

sfctl application delete --application-id CounterServiceApplication
sfctl application unprovision --application-type-name CounterServiceApplicationType --application-type-version 1.0.0
sfctl store delete --content-path CounterServiceApplication