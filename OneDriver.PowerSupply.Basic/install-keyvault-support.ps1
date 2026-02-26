# Optional: Add Azure Key Vault Support

# Install required packages
Write-Host "Installing Azure Key Vault packages..." -ForegroundColor Green
dotnet add OneDriver.PowerSupply.Basic.GrpcHost package Azure.Identity
dotnet add OneDriver.PowerSupply.Basic.GrpcHost package Azure.Security.KeyVault.Secrets

Write-Host "`n✓ Azure Key Vault packages installed" -ForegroundColor Green
Write-Host "`nTo use Key Vault:" -ForegroundColor Yellow
Write-Host "1. Create a Key Vault in Azure"
Write-Host "2. Store your connection string as a secret"
Write-Host "3. Update Program.cs to use AzureKeyVaultExtensions"
Write-Host "`nSee AZURE_SETUP.md for detailed instructions"
