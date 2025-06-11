# PowerShell script to setup Dalamud development environment
param(
    [Parameter(Mandatory=$false)]
    [switch]$Force
)

Write-Host "Dalamud Development Environment Setup" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

$dalamudPath = "$env:APPDATA\XIVLauncher\addon\Hooks\dev"

# Check if already exists
if ((Test-Path $dalamudPath) -and -not $Force) {
    Write-Host "`nDalamud development environment already exists at:" -ForegroundColor Yellow
    Write-Host $dalamudPath -ForegroundColor White
    
    $requiredFiles = @(
        "$dalamudPath\Dalamud.dll",
        "$dalamudPath\ImGui.NET.dll",
        "$dalamudPath\FFXIVClientStructs.dll",
        "$dalamudPath\Lumina.dll",
        "$dalamudPath\Newtonsoft.Json.dll"
    )
    
    $missingFiles = @()
    foreach ($file in $requiredFiles) {
        if (Test-Path $file) {
            Write-Host "✓ $(Split-Path $file -Leaf)" -ForegroundColor Green
        } else {
            Write-Host "✗ $(Split-Path $file -Leaf)" -ForegroundColor Red
            $missingFiles += $file
        }
    }
    
    if ($missingFiles.Count -eq 0) {
        Write-Host "`n✓ All required files are present!" -ForegroundColor Green
        Write-Host "Use -Force to reinstall anyway." -ForegroundColor Gray
        exit 0
    } else {
        Write-Host "`n⚠ Some files are missing. Proceeding with setup..." -ForegroundColor Yellow
    }
}

try {
    # Check if XIVLauncher is installed, if not, offer to install it
    $xivLauncherPath = "$env:LOCALAPPDATA\XIVLauncher\XIVLauncher.exe"
    if (-not (Test-Path $xivLauncherPath)) {
        Write-Host "`nXIVLauncher not found. Installing XIVLauncher..." -ForegroundColor Yellow

        # Download XIVLauncher Setup.exe
        $setupUrl = "https://github.com/goatcorp/FFXIVQuickLauncher/releases/latest/download/Setup.exe"
        $setupPath = "$env:TEMP\XIVLauncher-Setup.exe"

        Write-Host "Downloading XIVLauncher Setup from: $setupUrl" -ForegroundColor Gray
        Invoke-WebRequest -Uri $setupUrl -OutFile $setupPath -UseBasicParsing

        Write-Host "Installing XIVLauncher..." -ForegroundColor Yellow
        Write-Host "Note: This will install XIVLauncher on your system." -ForegroundColor Gray

        # Install XIVLauncher silently
        Start-Process -FilePath $setupPath -ArgumentList "/S" -Wait -NoNewWindow

        # Wait for installation to complete
        Start-Sleep -Seconds 5

        # Clean up setup file
        Remove-Item $setupPath -Force -ErrorAction SilentlyContinue

        # Verify installation
        if (Test-Path $xivLauncherPath) {
            Write-Host "✓ XIVLauncher installed successfully" -ForegroundColor Green
        } else {
            Write-Warning "XIVLauncher installation may have failed"
        }
    } else {
        Write-Host "`n✓ XIVLauncher already installed" -ForegroundColor Green
    }

    # Create directory structure
    Write-Host "`nCreating Dalamud directory structure..." -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $dalamudPath -Force | Out-Null
    Write-Host "✓ Created: $dalamudPath" -ForegroundColor Green

    # Download latest Dalamud development files
    Write-Host "`nDownloading Dalamud development files..." -ForegroundColor Yellow
    $dalamudUrl = "https://github.com/goatcorp/Dalamud/releases/latest/download/latest.zip"
    $tempZip = "$env:TEMP\dalamud-latest.zip"

    # Remove existing temp file if it exists
    if (Test-Path $tempZip) {
        Remove-Item $tempZip -Force
    }

    Write-Host "Downloading from: $dalamudUrl" -ForegroundColor Gray
    Invoke-WebRequest -Uri $dalamudUrl -OutFile $tempZip -UseBasicParsing
    Write-Host "✓ Downloaded to: $tempZip" -ForegroundColor Green

    # Extract files
    Write-Host "`nExtracting Dalamud files..." -ForegroundColor Yellow
    Expand-Archive -Path $tempZip -DestinationPath $dalamudPath -Force
    Write-Host "✓ Extracted to: $dalamudPath" -ForegroundColor Green

    # Clean up temp file
    Remove-Item $tempZip -Force
    
    # Verify installation
    Write-Host "`nVerifying installation..." -ForegroundColor Yellow
    $requiredFiles = @(
        "$dalamudPath\Dalamud.dll",
        "$dalamudPath\ImGui.NET.dll",
        "$dalamudPath\FFXIVClientStructs.dll",
        "$dalamudPath\Lumina.dll",
        "$dalamudPath\Lumina.Excel.dll",
        "$dalamudPath\Newtonsoft.Json.dll",
        "$dalamudPath\Serilog.dll"
    )
    
    $foundFiles = 0
    foreach ($file in $requiredFiles) {
        if (Test-Path $file) {
            Write-Host "✓ $(Split-Path $file -Leaf)" -ForegroundColor Green
            $foundFiles++
        } else {
            Write-Host "✗ $(Split-Path $file -Leaf)" -ForegroundColor Red
        }
    }
    
    Write-Host "`nSetup Summary:" -ForegroundColor Cyan
    Write-Host "Found $foundFiles of $($requiredFiles.Count) required files" -ForegroundColor White
    
    if ($foundFiles -eq $requiredFiles.Count) {
        Write-Host "✓ Dalamud development environment setup complete!" -ForegroundColor Green
        Write-Host "`nYou can now build Dalamud plugins locally." -ForegroundColor White
    } elseif ($foundFiles -gt 0) {
        Write-Host "⚠ Partial setup completed. Some files may be missing." -ForegroundColor Yellow
        Write-Host "This might still work for basic development." -ForegroundColor Gray
    } else {
        Write-Host "✗ Setup failed. No required files found." -ForegroundColor Red
        exit 1
    }
    
    # Test build if OnePiece project exists
    if (Test-Path "OnePiece\OnePiece.csproj") {
        Write-Host "`nTesting build..." -ForegroundColor Yellow
        try {
            $buildResult = dotnet build OnePiece\OnePiece.csproj -c Release --verbosity quiet 2>&1
            if ($LASTEXITCODE -eq 0) {
                Write-Host "✓ Test build successful!" -ForegroundColor Green
            } else {
                Write-Host "⚠ Test build failed, but Dalamud environment is set up." -ForegroundColor Yellow
                Write-Host "Build error: $buildResult" -ForegroundColor Gray
            }
        } catch {
            Write-Host "⚠ Could not test build: $_" -ForegroundColor Yellow
        }
    }
    
} catch {
    Write-Host "`n✗ Setup failed with error:" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    exit 1
}

Write-Host "`nNext steps:" -ForegroundColor Cyan
Write-Host "1. Try building your Dalamud plugin project:" -ForegroundColor White
Write-Host "   dotnet build OnePiece.sln -c Release" -ForegroundColor Gray
Write-Host "2. If you encounter issues, check the Dalamud documentation" -ForegroundColor White
Write-Host "3. For CI/CD, the GitHub Actions workflow will handle this automatically" -ForegroundColor White
Write-Host "4. You can now develop and test Dalamud plugins locally" -ForegroundColor White

Write-Host "`nUseful commands:" -ForegroundColor Cyan
Write-Host "• Check release readiness: .\.github\scripts\check-release-ready.ps1" -ForegroundColor Gray
Write-Host "• Update version: .\.github\scripts\update-version.ps1 -Version '1.0.1.0'" -ForegroundColor Gray
Write-Host "• Re-run this setup: .\.github\scripts\setup-dalamud-dev.ps1 -Force" -ForegroundColor Gray
