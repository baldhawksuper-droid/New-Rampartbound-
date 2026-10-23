@echo off
setlocal enabledelayedexpansion

echo ========================================================
echo   Rampartbound: Merge Defense - Android APK Build Tool
echo ========================================================
echo.

:: 1. Cari path Unity Editor di sistem
set UNITY_EXE=""

if exist "C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe" (
    set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe"
) else if exist "C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe" (
    set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\6000.6.6f1\Editor\Unity.exe"
) else if exist "C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe" (
    set UNITY_EXE="C:\Program Files\Unity\Hub\Editor\2022.3.50f1\Editor\Unity.exe"
) else (
    for /d %%i in ("C:\Program Files\Unity\Hub\Editor\*") do (
        if exist "%%i\Editor\Unity.exe" (
            set UNITY_EXE="%%i\Editor\Unity.exe"
        )
    )
)

if %UNITY_EXE%=="" (
    echo [INFO] Unity Editor lokal tidak ditemukan secara otomatis di Program Files.
    echo [INFO] Menjalankan build via GitHub Actions (Cloud CI/CD)...
    where gh >nul 2>nul
    if %ERRORLEVEL% EQU 0 (
        echo [INFO] Memicu workflow GitHub Actions menggunakan GitHub CLI...
        gh workflow run build-android-apk.yml -f buildType=apk
        echo [SUKSES] Workflow Build Android APK telah dipicu di GitHub!
        echo Cek status progress di: gh run list --workflow=build-android-apk.yml
    ) else (
        echo [PANDUAN] Silakan buka tab Actions di GitHub repository:
        echo https://github.com/baldhawksuper-droid/Rampartbound-Unity/actions
        echo dan klik 'Run workflow' pada workflow 'Build Android APK & Release'.
    )
    goto finish
)

echo [INFO] Ditemukan Unity Editor di: %UNITY_EXE%
echo [INFO] Memulai kompilasi Android Sideload APK...
set PROJECT_PATH=%~dp0..
set LOG_FILE=%PROJECT_PATH%\Builds\Android\build_apk.log

if not exist "%PROJECT_PATH%\Builds\Android" mkdir "%PROJECT_PATH%\Builds\Android"

%UNITY_EXE% -batchmode -quit -projectPath "%PROJECT_PATH%" -executeMethod Rampartbound.Editor.RampartboundBuildPipeline.BuildAndroidAPK -logFile "%LOG_FILE%"

if %ERRORLEVEL% NEQ 0 (
    echo [ERROR] Kompilasi gagal! Silakan periksa log di: %LOG_FILE%
) else (
    echo [SUKSES] APK berhasil dibangun!
    echo Berkas APK tersimpan di: %PROJECT_PATH%\Builds\Android\
)

:finish
echo.
pause
