# PowerShell script to update version across all files
param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$false)]
    [string]$Changelog = "- Bug fixes and improvements"
)

# Validate version format
if ($Version -notmatch '^\d+\.\d+\.\d+\.\d+$') {
    Write-Error "Version must be in format X.Y.Z.W (e.g., 1.0.1.0)"
    exit 1
}

Write-Host "Updating version to: $Version" -ForegroundColor Green

# Update OnePiece.csproj
$csprojPath = "OnePiece/OnePiece.csproj"
if (Test-Path $csprojPath) {
    $content = Get-Content $csprojPath -Raw
    $content = $content -replace '<Version>[\d\.]+</Version>', "<Version>$Version</Version>"
    Set-Content $csprojPath $content -NoNewline
    Write-Host "✓ Updated $csprojPath" -ForegroundColor Green
} else {
    Write-Warning "OnePiece.csproj not found"
}

# Update repo.json
$repoJsonPath = "repo.json"
if (Test-Path $repoJsonPath) {
    $repoJson = Get-Content $repoJsonPath -Raw | ConvertFrom-Json
    $timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds()
    
    $plugin = $repoJson[0]
    $plugin.AssemblyVersion = $Version
    $plugin.LastUpdate = $timestamp
    $plugin.Changelog = $Changelog
    
    $repoJson | ConvertTo-Json -Depth 10 | Set-Content $repoJsonPath -NoNewline
    Write-Host "✓ Updated $repoJsonPath" -ForegroundColor Green
    Write-Host "  - AssemblyVersion: $Version" -ForegroundColor Gray
    Write-Host "  - LastUpdate: $timestamp" -ForegroundColor Gray
    Write-Host "  - Changelog: $Changelog" -ForegroundColor Gray
} else {
    Write-Warning "repo.json not found"
}

Write-Host "`nVersion update completed!" -ForegroundColor Green
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Review the changes" -ForegroundColor White
Write-Host "2. Commit and push to trigger the release workflow" -ForegroundColor White
Write-Host "3. Or use GitHub Actions to trigger the release manually" -ForegroundColor White
