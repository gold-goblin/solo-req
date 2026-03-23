param([string]$Version = "")

$project = "SoloReq/SoloReq/SoloReq.csproj"
$nugetSource = "https://api.nuget.org/v3/index.json"

# Функция для чтения текущей версии из csproj
function Get-CurrentVersion {
    [xml]$csproj = Get-Content $project
    return $csproj.Project.PropertyGroup.Version
}

# Функция для обновления версии в csproj
function Set-ProjectVersion {
    param([string]$NewVersion)
    
    [xml]$csproj = Get-Content $project
    $csproj.Project.PropertyGroup.Version = $NewVersion
    $csproj.Save((Resolve-Path $project))
    Write-Host "  -> Version updated to $NewVersion in $project" -ForegroundColor Green
}

# Получаем текущую версию
$currentVersion = Get-CurrentVersion
Write-Host "Current project version: $currentVersion" -ForegroundColor Gray

# Запрашиваем версию, если не передана параметром
if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = Read-Host "Enter version for build (e.g., 1.2.0)"
    
    # Проверяем формат версии
    if ([string]::IsNullOrWhiteSpace($Version)) {
        Write-Host "Version cannot be empty. Exiting." -ForegroundColor Red
        exit 1
    }
}

# Валидация формата версии (semver-like)
if ($Version -notmatch '^\d+\.\d+(\.\d+)?$') {
    Write-Host "Invalid version format. Expected: X.Y or X.Y.Z (e.g., 1.2.0)" -ForegroundColor Red
    exit 1
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Building SoloReq v$Version" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

# Обновляем версию в проекте
Write-Host "[0/3] Updating project version..." -ForegroundColor Yellow
Set-ProjectVersion -NewVersion $Version

# Очищаем старые папки publish
Write-Host "`n[1/3] Cleaning old publish directories..." -ForegroundColor Yellow
if (Test-Path "publish") {
    Remove-Item -Recurse -Force "publish" -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Force -Path "publish/self-contained" | Out-Null
New-Item -ItemType Directory -Force -Path "publish/framework" | Out-Null

# Self-contained
Write-Host "`n[2/3] Self-contained build..." -ForegroundColor Yellow
dotnet publish $project -c Release -r win-x64 --self-contained -p:PublishSingleFile=true "-p:Version=$Version" "-p:RestoreAdditionalProjectSources=$nugetSource" -o publish/self-contained

if ($LASTEXITCODE -ne 0) { 
    Write-Host "Self-contained build failed!" -ForegroundColor Red
    # Восстанавливаем исходную версию в случае ошибки
    Set-ProjectVersion -NewVersion $currentVersion
    exit 1 
}

# Создаём архив (ZIP по умолчанию, для RAR используйте WinRAR)
$selfContainedZip = "publish/SoloReq-v$Version-win-x64.zip"

# Проверяем наличие WinRAR для создания RAR
$winrar = Get-Command "WinRAR.exe" -ErrorAction SilentlyContinue
if ($winrar) {
    $selfContainedRar = "publish/SoloReq-v$Version-win-x64.rar"
    Write-Host "  Creating RAR archive with WinRAR..." -ForegroundColor Yellow
    & WinRAR.exe a -r -ep1 -m5 "$selfContainedRar" "publish/self-contained\*" > $null
    Write-Host "  -> publish/SoloReq-v$Version-win-x64.rar" -ForegroundColor Green
} else {
    Write-Host "  WinRAR not found, creating ZIP archive..." -ForegroundColor Yellow
    Compress-Archive -Path "publish/self-contained\*" -DestinationPath $selfContainedZip -Force
    Write-Host "  -> publish/SoloReq-v$Version-win-x64.zip" -ForegroundColor Green
    Write-Host "  (To create RAR archive manually, use: WinRAR a -r archive.rar publish/self-contained\*)" -ForegroundColor DarkGray
}

# Framework-dependent
Write-Host "`n[3/3] Framework-dependent build..." -ForegroundColor Yellow
dotnet publish $project -c Release -r win-x64 --no-self-contained -p:PublishSingleFile=true "-p:Version=$Version" "-p:RestoreAdditionalProjectSources=$nugetSource" -o publish/framework

if ($LASTEXITCODE -ne 0) { 
    Write-Host "Framework-dependent build failed!" -ForegroundColor Red
    # Восстанавливаем исходную версию в случае ошибки
    Set-ProjectVersion -NewVersion $currentVersion
    exit 1 
}

# Создаём архив для framework-версии
if ($winrar) {
    $frameworkRar = "publish/SoloReq-v$Version-win-x64-framework.rar"
    Write-Host "  Creating RAR archive with WinRAR..." -ForegroundColor Yellow
    & WinRAR.exe a -r -ep1 -m5 "$frameworkRar" "publish/framework\*" > $null
    Write-Host "  -> publish/SoloReq-v$Version-win-x64-framework.rar" -ForegroundColor Green
} else {
    $frameworkZip = "publish/SoloReq-v$Version-win-x64-framework.zip"
    Write-Host "  WinRAR not found, creating ZIP archive..." -ForegroundColor Yellow
    Compress-Archive -Path "publish/framework\*" -DestinationPath $frameworkZip -Force
    Write-Host "  -> publish/SoloReq-v$Version-win-x64-framework.zip" -ForegroundColor Green
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "  Build completed successfully!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Version: $Version" -ForegroundColor Gray
Write-Host "`nFiles to upload to GitHub Release:" -ForegroundColor Cyan

if ($winrar) {
    Write-Host "  - publish/SoloReq-v$Version-win-x64.rar (self-contained)" -ForegroundColor White
    Write-Host "  - publish/SoloReq-v$Version-win-x64-framework.rar (framework-dependent)" -ForegroundColor White
} else {
    Write-Host "  - publish/SoloReq-v$Version-win-x64.zip (self-contained)" -ForegroundColor White
    Write-Host "  - publish/SoloReq-v$Version-win-x64-framework.zip (framework-dependent)" -ForegroundColor White
}

Write-Host "`nNOTE: Project version in .csproj updated to $Version" -ForegroundColor Gray
