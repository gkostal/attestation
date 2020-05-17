@echo off
rem 
rem script to verify all example remote attestation quotes 
rem
dotnet run ../genquotes/quotes/enclave.info.debug.json               sharedwus.us.test.attest.azure.net    true
dotnet run ../genquotes/quotes/enclave.info.release.json             sharedwus.us.test.attest.azure.net    true
dotnet run ../genquotes/quotes/enclave.info.prodid.json              sharedwus.us.test.attest.azure.net    true
dotnet run ../genquotes/quotes/enclave.info.securityversion.json     sharedwus.us.test.attest.azure.net    true
