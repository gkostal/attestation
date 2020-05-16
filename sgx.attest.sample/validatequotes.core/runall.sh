#!/bin/bash
# 
# Script to verify all example remote attestation quotes
#
dotnet run ../genquotes/quotes/enclave.info.debug.json              sharedwus.us.test.attest.azure.net    false
dotnet run ../genquotes/quotes/enclave.info.release.json            sharedwus.us.test.attest.azure.net    false
dotnet run ../genquotes/quotes/enclave.info.prodid.json             sharedwus.us.test.attest.azure.net    false
dotnet run ../genquotes/quotes/enclave.info.securityversion.json    sharedwus.us.test.attest.azure.net    false

