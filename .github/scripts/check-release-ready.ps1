# PowerShell script to check if the project is ready for release
param(
    [Parameter(Mandatory=$false)]
    [string]$TargetVersion
)

Write-Host "OnePiece Plugin Release Readiness Check" -ForegroundColor Cyan
Write-Host "=======================================" -ForegroundColor Cyan

$errors = @()
$warnings = @()

# Check if files exist
$requiredFiles = @(
    "OnePiece/OnePiece.csproj",
    "repo.json",
    "OnePiece/OnePiece.json",
    "OnePiece/aetheryte.json"
)

Write-Host "`n1. Checking required files..." -ForegroundColor Yellow
foreach ($file in $requiredFiles) {
    if (Test-Path $file) {
        Write-Host "   ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $file" -ForegroundColor Red
        $errors += "Missing required file: $file"
    }
}

# Check version consistency
Write-Host "`n2. Checking version consistency..." -ForegroundColor Yellow
try {
    # Get version from csproj
    $csprojContent = Get-Content "OnePiece/OnePiece.csproj" -Raw
    if ($csprojContent -match '<Version>([\d\.]+)</Version>') {
        $csprojVersion = $matches[1]
        Write-Host "   OnePiece.csproj version: $csprojVersion" -ForegroundColor White
    } else {
        $errors += "Could not find version in OnePiece.csproj"
    }
    
    # Get version from repo.json
    $repoJson = Get-Content "repo.json" -Raw | ConvertFrom-Json
    $repoVersion = $repoJson[0].AssemblyVersion
    Write-Host "   repo.json version: $repoVersion" -ForegroundColor White
    
    if ($csprojVersion -eq $repoVersion) {
        Write-Host "   ✓ Versions are consistent" -ForegroundColor Green
    } else {
        $warnings += "Version mismatch: csproj ($csprojVersion) vs repo.json ($repoVersion)"
    }
    
    # Check target version if provided
    if ($TargetVersion) {
        if ($TargetVersion -notmatch '^\d+\.\d+\.\d+\.\d+$') {
            $errors += "Target version format invalid. Use X.Y.Z.W format"
        } else {
            Write-Host "   Target version: $TargetVersion" -ForegroundColor White
            if ($TargetVersion -eq $csprojVersion) {
                $warnings += "Target version same as current version"
            }
        }
    }
} catch {
    $errors += "Error checking versions: $_"
}

# Check XIVLauncher and Dalamud environment
Write-Host "`n3. Checking XIVLauncher and Dalamud environment..." -ForegroundColor Yellow

# Check XIVLauncher installation
$xivLauncherPath = "$env:LOCALAPPDATA\XIVLauncher\XIVLauncher.exe"
if (Test-Path $xivLauncherPath) {
    Write-Host "   ✓ XIVLauncher installed: $xivLauncherPath" -ForegroundColor Green
} else {
    Write-Host "   ⚠ XIVLauncher not found" -ForegroundColor Yellow
    $warnings += "XIVLauncher not installed. Run setup-dalamud-dev.ps1 to install it."
}

# Check Dalamud development environment
$dalamudPath = "$env:APPDATA\XIVLauncher\addon\Hooks\dev"
if (Test-Path $dalamudPath) {
    Write-Host "   ✓ Dalamud dev path exists: $dalamudPath" -ForegroundColor Green

    $dalamudFiles = @(
        "$dalamudPath\Dalamud.dll",
        "$dalamudPath\ImGui.NET.dll",
        "$dalamudPath\FFXIVClientStructs.dll",
        "$dalamudPath\Lumina.dll",
        "$dalamudPath\Newtonsoft.Json.dll"
    )

    foreach ($file in $dalamudFiles) {
        if (Test-Path $file) {
            Write-Host "   ✓ Found: $(Split-Path $file -Leaf)" -ForegroundColor Green
        } else {
            Write-Host "   ✗ Missing: $(Split-Path $file -Leaf)" -ForegroundColor Red
            $warnings += "Missing Dalamud file: $(Split-Path $file -Leaf)"
        }
    }
} else {
    Write-Host "   ⚠ Dalamud development environment not found" -ForegroundColor Yellow
    $warnings += "Dalamud development environment not found at $dalamudPath"
    Write-Host "   Run .\.github\scripts\setup-dalamud-dev.ps1 to set it up" -ForegroundColor Gray
}

# Check build
Write-Host "`n4. Testing build..." -ForegroundColor Yellow
try {
    $buildResult = dotnet build OnePiece.sln -c Release --verbosity quiet 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-Host "   ✓ Build successful" -ForegroundColor Green
    } else {
        Write-Host "   ✗ Build failed" -ForegroundColor Red
        $errors += "Build failed: $buildResult"
    }
} catch {
    $errors += "Error running build: $_"
}

# Check release files
Write-Host "`n5. Checking release output..." -ForegroundColor Yellow
$releaseDir = "OnePiece/bin/Release"
$releaseFiles = @(
    "$releaseDir/OnePiece.dll",
    "$releaseDir/OnePiece.json",
    "$releaseDir/ECommons.dll",
    "$releaseDir/aetheryte.json"
)

foreach ($file in $releaseFiles) {
    if (Test-Path $file) {
        Write-Host "   ✓ $file" -ForegroundColor Green
    } else {
        Write-Host "   ✗ $file" -ForegroundColor Red
        $errors += "Missing release file: $file"
    }
}

# Check for debug files (should not be present)
$debugFiles = @(
    "$releaseDir/ECommons.pdb",
    "$releaseDir/ECommons.xml"
)

foreach ($file in $debugFiles) {
    if (Test-Path $file) {
        Write-Host "   ⚠ Debug file present: $file" -ForegroundColor Yellow
        $warnings += "Debug file found in release: $file"
    }
}

# Check repo.json format
Write-Host "`n6. Validating repo.json..." -ForegroundColor Yellow
try {
    $repoJson = Get-Content "repo.json" -Raw | ConvertFrom-Json
    $plugin = $repoJson[0]
    
    $requiredFields = @('Author', 'Name', 'InternalName', 'AssemblyVersion', 'RepoUrl', 'DownloadLinkInstall', 'DownloadLinkUpdate')
    foreach ($field in $requiredFields) {
        if ($plugin.$field) {
            Write-Host "   ✓ $field" -ForegroundColor Green
        } else {
            Write-Host "   ✗ $field" -ForegroundColor Red
            $errors += "Missing required field in repo.json: $field"
        }
    }
} catch {
    $errors += "Invalid repo.json format: $_"
}

# Summary
Write-Host "`n" + "="*50 -ForegroundColor Cyan
Write-Host "SUMMARY" -ForegroundColor Cyan
Write-Host "="*50 -ForegroundColor Cyan

if ($errors.Count -eq 0 -and $warnings.Count -eq 0) {
    Write-Host "✓ Ready for release!" -ForegroundColor Green
    exit 0
} else {
    if ($errors.Count -gt 0) {
        Write-Host "`nERRORS:" -ForegroundColor Red
        foreach ($error in $errors) {
            Write-Host "  • $error" -ForegroundColor Red
        }
    }
    
    if ($warnings.Count -gt 0) {
        Write-Host "`nWARNINGS:" -ForegroundColor Yellow
        foreach ($warning in $warnings) {
            Write-Host "  • $warning" -ForegroundColor Yellow
        }
    }
    
    if ($errors.Count -gt 0) {
        Write-Host "`n✗ Not ready for release. Please fix errors above." -ForegroundColor Red
        exit 1
    } else {
        Write-Host "`n⚠ Ready for release with warnings." -ForegroundColor Yellow
        exit 0
    }
}
