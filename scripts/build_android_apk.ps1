<#
.SYNOPSIS
    Automated Android APK / AAB Build Script for Rampartbound: Merge Defense
.DESCRIPTION
    Script ini mencari instalasi Unity 6 / 2022.3 LTS di sistem Windows, lalu mengeksekusi
    metode RampartboundBuildPipeline.BuildAndroidAPK secara headless (batchmode).
    Jika Unity lokal tidak terpasang, script dapat memicu build via GitHub Actions secara otomatis.
#>

param(
    [ValidateSet("apk", "aab")]
    [string]$BuildType = "apk",
    [switch]$CloudCI
)

$ErrorActionPreference = "Stop"
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "  Rampartbound: Merge Defense - Android Build Automation" -ForegroundColor Cyan
Write-Host "========================================================" -ForegroundColor Cyan
Write-Host ""

$projectRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ($CloudCI) {
    Write-Host "[INFO] Mode Cloud CI dipilih. Memicu GitHub Actions..." -ForegroundColor Yellow
    gh workflow run build-android-apk.yml -f buildType=$BuildType
    Write-Host "[SUKSES] GitHub Actions workflow berhasil dipicu!" -ForegroundColor Green
    exit 0
}

# 1. Cari Unity.exe
$unityCandidates = @(
    "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe",
    "C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe"
)

$unityExe = $null
foreach ($path in $unityCandidates) {
    if (Test-Path $path) {
        $unityExe = $path
        break
    }
}

if (-not $unityExe) {
    $hubEditors = Get-ChildItem -Path "C:\Program Files\Unity\Hub\Editor" -ErrorAction SilentlyContinue
    foreach ($ed in $hubEditors) {
        $candidate = Join-Path $ed.FullName "Editor\Unity.exe"
        if (Test-Path $candidate) {
            $unityExe = $candidate
            break
        }
    }
}

if (-not $unityExe) {
    Write-Host "[INFO] Unity Editor lokal tidak terdeteksi di lokasi standar." -ForegroundColor Yellow
    Write-Host "[INFO] Memicu build cloud melalui GitHub Actions via gh CLI..." -ForegroundColor Yellow
    
    $ghExists = Get-Command gh -ErrorAction SilentlyContinue
    if ($ghExists) {
        gh workflow run build-android-apk.yml -f buildType=$BuildType
        Write-Host "[SUKSES] Workflow 'Build Android APK & Release' berhasil dipicu di GitHub!" -ForegroundColor Green
        Write-Host "Kunjungi: https://github.com/baldhawksuper-droid/Rampartbound-Unity/actions untuk melihat proses build." -ForegroundColor Cyan
    } else {
        Write-Host "[PANDUAN] Buka https://github.com/baldhawksuper-droid/Rampartbound-Unity/actions dan klik 'Run workflow'." -ForegroundColor Yellow
    }
    exit 0
}

Write-Host "[INFO] Menggunakan Unity Editor di: $unityExe" -ForegroundColor Green
$outputDir = Join-Path $projectRoot "Builds\Android"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
}

$logFile = Join-Path $outputDir "build_android.log"
$buildMethod = if ($BuildType -eq "aab") {
    "Rampartbound.Editor.RampartboundBuildPipeline.BuildGooglePlayAAB"
} else {
    "Rampartbound.Editor.RampartboundBuildPipeline.BuildAndroidAPK"
}

Write-Host "[INFO] Memulai proses kompilasi $BuildType via $buildMethod..." -ForegroundColor Cyan
Write-Host "[INFO] Log file: $logFile" -ForegroundColor Gray

$processArgs = @(
    "-batchmode",
    "-quit",
    "-projectPath", "`"$projectRoot`"",
    "-executeMethod", $buildMethod,
    "-logFile", "`"$logFile`""
)

$process = Start-Process -FilePath $unityExe -ArgumentList $processArgs -Wait -PassThru

if ($process.ExitCode -eq 0) {
    Write-Host "[SUKSES] Kompilasi Android selesai dengan kode 0!" -ForegroundColor Green
    Write-Host "File keluaran tersedia di direktori: $outputDir" -ForegroundColor Green
} else {
    Write-Host "[ERROR] Kompilasi gagal dengan kode keluar: $($process.ExitCode)" -ForegroundColor Red
    Write-Host "Silakan baca catatan error di: $logFile" -ForegroundColor Red
}
