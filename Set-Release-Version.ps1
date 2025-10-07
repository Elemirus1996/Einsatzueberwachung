# PowerShell Script zum Setzen der korrekten Release-Version
# Dieses Script synchronisiert alle Versionsangaben für ein Release

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    [switch]$Development = $false
)

Write-Host "🔧 Setting version to $Version (Development: $Development)" -ForegroundColor Green

# 1. Update VersionService.cs
$versionServicePath = "Services\VersionService.cs"
if (Test-Path $versionServicePath) {
    $content = Get-Content $versionServicePath -Raw
    
    # Parse version components
    $versionParts = $Version.Split('.')
    $major = $versionParts[0]
    $minor = if ($versionParts.Length -gt 1) { $versionParts[1] } else { "0" }
    $patch = if ($versionParts.Length -gt 2) { $versionParts[2] } else { "0" }
    
    # Update version constants
    $content = $content -replace 'private const string MAJOR_VERSION = "[^"]*"', "private const string MAJOR_VERSION = `"$major`""
    $content = $content -replace 'private const string MINOR_VERSION = "[^"]*"', "private const string MINOR_VERSION = `"$minor`""
    $content = $content -replace 'private const string PATCH_VERSION = "[^"]*"', "private const string PATCH_VERSION = `"$patch`""
    
    # Update development flag
    $developmentFlag = if ($Development) { "true" } else { "false" }
    $content = $content -replace 'private const bool IS_DEVELOPMENT_VERSION = (true|false)', "private const bool IS_DEVELOPMENT_VERSION = $developmentFlag"
    
    Set-Content $versionServicePath $content -Encoding UTF8
    Write-Host "✅ Updated VersionService.cs" -ForegroundColor Green
} else {
    Write-Host "❌ VersionService.cs not found" -ForegroundColor Red
}

# 2. Update .csproj file
$csprojPath = "Einsatzueberwachung.csproj"
if (Test-Path $csprojPath) {
    [xml]$csproj = Get-Content $csprojPath
    
    $assemblyVersion = "$Version.0"
    
    # Update PropertyGroup elements
    $propertyGroup = $csproj.Project.PropertyGroup | Where-Object { $_.AssemblyVersion -or $_.Version }
    if ($propertyGroup) {
        $propertyGroup.AssemblyTitle = "Einsatzüberwachung Professional v$Version"
        $propertyGroup.AssemblyProduct = "Einsatzüberwachung Professional v$Version"
        $propertyGroup.AssemblyVersion = $assemblyVersion
        $propertyGroup.FileVersion = $assemblyVersion
        $propertyGroup.Version = $Version
    }
    
    $csproj.Save($csprojPath)
    Write-Host "✅ Updated Einsatzueberwachung.csproj" -ForegroundColor Green
} else {
    Write-Host "❌ Einsatzueberwachung.csproj not found" -ForegroundColor Red
}

# 3. Update Create-Release-Tag scripts
$createTagBat = "Create-Release-Tag.bat"
if (Test-Path $createTagBat) {
    $content = Get-Content $createTagBat -Raw
    $content = $content -replace 'set VERSION=[^\r\n]*', "set VERSION=$Version"
    Set-Content $createTagBat $content -Encoding ASCII
    Write-Host "✅ Updated Create-Release-Tag.bat" -ForegroundColor Green
}

$createTagPs1 = "Create-Release-Tag.ps1"
if (Test-Path $createTagPs1) {
    $content = Get-Content $createTagPs1 -Raw
    $content = $content -replace '\$version = "[^"]*"', "`$version = `"$Version`""
    Set-Content $createTagPs1 $content -Encoding UTF8
    Write-Host "✅ Updated Create-Release-Tag.ps1" -ForegroundColor Green
}

# 4. Show summary
Write-Host "`n📋 Version Update Summary:" -ForegroundColor Cyan
Write-Host "   Version: $Version" -ForegroundColor White
Write-Host "   Assembly Version: $Version.0" -ForegroundColor White
Write-Host "   Development Mode: $Development" -ForegroundColor White

Write-Host "`n🚀 Next steps for release:" -ForegroundColor Yellow
Write-Host "   1. Build and test the application" -ForegroundColor Gray
Write-Host "   2. Run 'Create-Release-Tag.bat' or 'Create-Release-Tag.ps1'" -ForegroundColor Gray
Write-Host "   3. Push the tag to GitHub to trigger automatic release" -ForegroundColor Gray

# 5. Validate changes
Write-Host "`n🔍 Validating changes..." -ForegroundColor Cyan
try {
    dotnet build --configuration Release --verbosity quiet
    if ($LASTEXITCODE -eq 0) {
        Write-Host "✅ Build successful with new version" -ForegroundColor Green
    } else {
        Write-Host "❌ Build failed - please check for errors" -ForegroundColor Red
    }
} catch {
    Write-Host "⚠️ Could not validate build" -ForegroundColor Yellow
}

Write-Host "`n✅ Version update completed!" -ForegroundColor Green
