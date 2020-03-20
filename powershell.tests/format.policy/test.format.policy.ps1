. .\format.policy.ps1

$sgxPolicy = Get-Content .\sgx.policy.txt -Raw
Format-Maa-Policy -Policy $sgxPolicy
    