#!/bin/bash
# 
# Script to verify all example remote attestation quotes
#

while :
do

dotnet run ../genquotes/quotes/enclave.info.debug.json              gnkaaa555.uks.test.attest.azure.net    false
dotnet run ../genquotes/quotes/enclave.info.release.json            gnkaaa555.uks.test.attest.azure.net    false
dotnet run ../genquotes/quotes/enclave.info.prodid.json             gnkaaa555.uks.test.attest.azure.net    false
dotnet run ../genquotes/quotes/enclave.info.securityversion.json    gnkaaa555.uks.test.attest.azure.net    false

done

