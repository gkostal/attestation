#!/bin/bash

RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m'

echo -e "${RED}SGX${YELLOW} attestation; 1 connection limited to 1 RPS${NC}"
dotnet run -- -p sharedwus.wus.attest.azure.net -c 1 -r 1 -a GetOpenIdConfiguration -s

echo -e "${RED}SGX${YELLOW} attestation; 1 connection unlimited${NC}"
dotnet run -- -p sharedwus.wus.attest.azure.net -c 1 -r 9999 -a GetOpenIdConfiguration -s
