@echo off
rem 
rem script to verify all example remote attestation quotes 
rem
dotnet run ../genquotes/quotes/enclave.info.debug.json               tradewinds.us.test.attest.azure.net   false
dotnet run ../genquotes/quotes/enclave.info.release.json             tradewinds.us.test.attest.azure.net   false
dotnet run ../genquotes/quotes/enclave.info.prodid.json              tradewinds.us.test.attest.azure.net   false
dotnet run ../genquotes/quotes/enclave.info.securityversion.json     tradewinds.us.test.attest.azure.net   false