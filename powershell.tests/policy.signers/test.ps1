# Setup variables
Write-Host ""
Write-Host "Init: Setting up environment variables" -ForegroundColor Cyan
$basename = "psigntest"
$rgname = "aztest-resourcegroup"
$location = "East US"
$sampleDir = "..\signed.data.for.test"

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

Write-Host ""
Write-Host "Creating attestation provider with signing certs" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$providerYesCerts = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location -PolicySignersCertificateFile $sampleDir\cert1.pem
$providerYesCerts
Write-Host "Attestation provider without certs created.  ResourceId is $($providerYesCerts.Id)" -ForegroundColor Cyan

try
{
    Write-Host ""
    Write-Host "Retrieving signing certs for attestion provider with none" -ForegroundColor Cyan
    $providerNoCerts | Get-AzAttestationPolicySigners

    Write-Host ""
    Write-Host "Retrieving signing certs for attestion provider with one" -ForegroundColor Cyan
    $providerYesCerts | Get-AzAttestationPolicySigners

    Write-Host ""
    Write-Host "Adding a new trusted signing certificate" -ForegroundColor Cyan
    $trustedSigner = Get-Content -Path $sampleDir\cert2.signed.txt
    $providerYesCerts | Add-AzAttestationPolicySigner -Signer $trustedSigner

    Write-Host ""
    Write-Host "Removing the new trusted signing certificate" -ForegroundColor Cyan
    $trustedSigner = Get-Content -Path $sampleDir\cert2.signed.txt
    $providerYesCerts | Remove-AzAttestationPolicySigner -Signer $trustedSigner
}
catch
{
    Write-Host ""
    Write-Host "Exception caught!" -ForegroundColor Red
    $PSItem.Exception.Message
}

# Delete test attestation providers
$providerNoCerts | Remove-AzAttestation
Write-Host ""
Write-Host "Removed ResourceId $($providerNoCerts.Id)" -ForegroundColor Cyan
$providerYesCerts | Remove-AzAttestation
Write-Host ""
Write-Host "Removed ResourceId $($providerYesCerts.Id)" -ForegroundColor Cyan
