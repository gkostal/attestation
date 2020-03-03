function Test-Policy {
    Param ($name, $rgname, $id, $signedTest)

    Write-Host ""
    Write-Host "Testing policy for provider named $name.  Signed = $signedTest" -ForegroundColor Cyan

    Write-Host ""
    Write-Host "Init: Reading in sample policy files" -ForegroundColor Cyan
    if ($signedTest) {
        $resetPolicy = Get-Content -Path $signedSampleDir\reset.policy.txt.signed.txt
    } else {
        $resetPolicy = Get-Content -Path $unsignedSampleDir\reset.policy.txt
    }

    try
    {
        "sgxenclave", "openenclave", "cyrescomponent", "vsmenclave" | % {
        #"SgxEnclave" | % {
            $tee = $_
            Write-Host ""
            Write-Host "Testing Tee type $tee" -ForegroundColor Cyan

            if ($signedTest) {
                $customPolicy = Get-Content -Path $signedSampleDir\custom.$tee.policy.txt.signed.txt
            } else {
                $customPolicy = Get-Content -Path $unsignedSampleDir\custom.$tee.policy.txt
            }
            Write-Host "Custom policy for $tee" -ForegroundColor Cyan
            Write-Host "$customPolicy" -ForegroundColor Cyan
        
            Write-Host "Test: Get $tee policy by Name" -ForegroundColor Green
            Get-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee

            Write-Host "Test: Get $tee policy by ResourceId" -ForegroundColor Green
            Get-AzAttestationPolicy -ResourceId $id  -Tee $tee

            Write-Host "Test: Set custom $tee policy by Name" -ForegroundColor Green
            Set-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $customPolicy
            Get-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee

            Write-Host "Test: Set custom $tee policy by ResourceId" -ForegroundColor Green
            Set-AzAttestationPolicy -ResourceId $id -Tee $tee -Policy $customPolicy
            Get-AzAttestationPolicy -ResourceId $id -Tee $tee

            Write-Host "Test: Set custom $tee policy with -PassThru" -ForegroundColor Green
            Set-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $customPolicy -PassThru

            Write-Host "Test: Set custom $tee policy with -WhatIf" -ForegroundColor Green
            Set-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $customPolicy -WhatIf

            if ($testConfirm){
                Write-Host "Test: Set custom $tee policy with -Confirm" -ForegroundColor Green
                Set-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $customPolicy -Confirm
            }

            Write-Host "Test: Reset $tee policy by Name" -ForegroundColor Green
            Reset-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $resetPolicy
            Get-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee

            Write-Host "Test: Reset $tee policy by ResourceId" -ForegroundColor Green
            Reset-AzAttestationPolicy -ResourceId $id -Tee $tee -Policy $resetPolicy
            Get-AzAttestationPolicy -ResourceId $id -Tee $tee

            if (-Not $signedTest)
            {
                Write-Host "Test:  Reset $tee policy without -Policy (it should be optional)" -ForegroundColor Green
                Reset-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee 
            }

            Write-Host "Test:  Reset $tee policy with -PassThru" -ForegroundColor Green
            Reset-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $resetPolicy -PassThru

            Write-Host "Test:  Reset $tee policy with -WhatIf" -ForegroundColor Green
            Reset-AzAttestationPolicy -Name $name -ResourceGroupName $rgname -Tee $tee -Policy $resetPolicy -WhatIf

            if ($testConfirm){
                Write-Host "Test:  Reset $tee policy with -Confirm" -ForegroundColor Green
                Reset-AzAttestationPolicy -Name $aname -ResourceGroupName $rgname -Tee $tee -Policy $resetPolicy -Confirm
            }

            Write-Host "Test: Piping into Get-AzAttestionPolicy for $tee" -ForegroundColor Blue
            $a = Get-AzAttestation -Name $aname -ResourceGroupName $rgname
            $a | Get-AzAttestationPolicy -Tee $tee

            Write-Host "Test: Piping into Set-AzAttestionPolicy for $tee" -ForegroundColor Blue
            $a | Set-AzAttestationPolicy -Tee $tee -Policy $customPolicy
            $a | Get-AzAttestationPolicy -Tee $tee

            Write-Host "Test: Piping into Reset-AzAttestionPolicy for $tee" -ForegroundColor Blue
            $a | Reset-AzAttestationPolicy -Tee $tee -Policy $resetPolicy
            $a | Get-AzAttestationPolicy -Tee $tee
        }
    }
    catch
    {
        Write-Host ""
        Write-Host "Exception caught!" -ForegroundColor Red
        $PSItem.Exception.Message
    }
}

# Setup variables
Write-Host "Init: Setting up environment variables" -ForegroundColor Cyan
$basename = "policytest"
$rgname = "aztest-resourcegroup"
$location = "East US"
$testConfirm = $false
$unsignedSampleDir = "..\unsigned.data.for.test"
$signedSampleDir = "..\signed.data.for.test"

# Create resource group if needed
New-AzResourceGroup -Name $rgname -Location $location -ErrorAction SilentlyContinue -Force

# Create test attestation providers
Write-Host ""
Write-Host "Creating attestation provider without signing certs" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$providerNoCerts = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location
$providerNoCerts
Write-Host "Attestation provider without certs created.  ResourceId is $($providerNoCerts.Id)" -ForegroundColor Cyan

Test-Policy $aname $rgname $providerNoCerts.Id $false

Write-Host ""
Write-Host "Creating attestation provider with signing certs" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$providerYesCerts = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location -PolicySignersCertificateFile $signedSampleDir\cert1.pem
$providerYesCerts
Write-Host "Attestation provider without certs created.  ResourceId is $($providerYesCerts.Id)" -ForegroundColor Cyan

Test-Policy $aname $rgname $providerNoCerts.Id $true

# Delete test attestation providers
$providerNoCerts | Remove-AzAttestation
Write-Host ""
Write-Host "Removed ResourceId $($providerNoCerts.Id)" -ForegroundColor Cyan
$providerYesCerts | Remove-AzAttestation
Write-Host ""
Write-Host "Removed ResourceId $($providerYesCerts.Id)" -ForegroundColor Cyan
