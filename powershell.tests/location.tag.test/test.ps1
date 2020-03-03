# Setup variables
Write-Host "Init: Setting up environment variables" -ForegroundColor Cyan
$basename = "loctagtest"
$rgname = "aztest-resourcegroup"
$location = "East US"

# Create resource group if needed
New-AzResourceGroup -Name $rgname -Location $location -ErrorAction SilentlyContinue -Force

Write-Host ""
Write-Host "Test: Create attestation provider in a location, no tags" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$myprovider = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location
$myprovider
Write-Host "Test Success: ResourceId is $($myprovider.Id)" -ForegroundColor Green
$myprovider | Remove-AzAttestation

Write-Host ""
Write-Host "Test: Create attestation provider in a location, with tags" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$myprovider = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location -Tag @{Key1="value1";Key2="value2"}
$myprovider
Write-Host "Test Success: ResourceId is $($myprovider.Id)" -ForegroundColor Green
$myprovider | Remove-AzAttestation

Write-Host ""
Write-Host "Test: Create attestation provider in a location, max tags" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$tmt = @{}
1..50 | % {
    $tmt += @{"Key$_"="Value$_"}
}
$myprovider = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location -Tag $tmt
$myprovider
Write-Host "Test Success: ResourceId is $($myprovider.Id)" -ForegroundColor Green
$myprovider2 = Get-AzAttestation -Name $aname -ResourceGroupName $rgname 
if ($myprovider2.Tags.Count -eq 50){
    Write-Host "Test Success: Get-AzAttestation returned provider with 50 tags!" -ForegroundColor Green
} else {
    Write-Host "Test Failure: Get-AzAttestation returned provider without 50 tags!" -ForegroundColor Red
}
if ($myprovider2.Location -eq $location){
    Write-Host "Test Success: Get-AzAttestation returned provider with correct location!" -ForegroundColor Green
} else {
    Write-Host "Test Failure: Get-AzAttestation returned provider with incorrect location!" -ForegroundColor Red
}
$myprovider | Remove-AzAttestation

Write-Host ""
Write-Host "Test: Create attestation provider in a location, too many tags" -ForegroundColor Cyan
$random = Get-Random -Minimum 1 -Maximum 1000000
$aname = "$basename$random"
$tmt = @{}
1..57 | % {
    $tmt += @{"Key$_"="Value$_"}
}
$myprovider = New-AzAttestation -Name $aname -ResourceGroupName $rgname -Location $location -Tag $tmt -ErrorAction SilentlyContinue -ErrorVariable CreateError
if ($CreateError){
    Write-Host $CreateError -ForegroundColor Green
    Write-Host "Test Success: New-AzAttestation failed with too many tags!" -ForegroundColor Green
} else {
    Write-Host "Test Failure: New-AzAttestation did not fail with too many tags!" -ForegroundColor Red
}


