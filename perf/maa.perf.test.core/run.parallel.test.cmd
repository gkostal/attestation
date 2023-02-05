@if "%_ECHO%" EQU "" echo off
SETLOCAL EnableDelayedExpansion

set _RP_LAUNCH=dotnet run -- -p

for %%s in (^
    "sharedeus.eus.test.attest.azure.net"^
    "sharedwus.wus.test.attest.azure.net"^
    "shareduks.uks.test.attest.azure.net"^
    "sharedeus2.eus2.test.attest.azure.net"^
    "sharedscus.scus.test.attest.azure.net")^
do (
    start %_RP_LAUNCH% %%s %*
)

