@if "%_ECHO%" EQU "" echo off

rem
rem Launch from parent directory 'script\run.parallel.prod.cmd'
rem 

SETLOCAL EnableDelayedExpansion

set _RP_LAUNCH=dotnet run -- -p

for %%s in (^
    "sharedcuse.cuse.attest.azure.net"^
    "sharedeus2.eus2.attest.azure.net"^
    "sharedcae.cae.attest.azure.net"^
    "sharedcus.cus.attest.azure.net"^
    "shareduks.uks.attest.azure.net"^
    "sharedeus.eus.attest.azure.net"^
    "sharedcac.cac.attest.azure.net"^
    "sharedukw.ukw.attest.azure.net"^
    "sharedneu.neu.attest.azure.net"^
    "sharedwus.wus.attest.azure.net"^
    "sharedweu.weu.attest.azure.net")^
do (
    start %_RP_LAUNCH% %%s %*
)

