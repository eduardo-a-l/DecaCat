@echo off
setlocal

set "UNITY_VERSION=6000.4.10f1"

if "%UNITY_EXE%"=="" set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\%UNITY_VERSION%\Editor\Unity.exe"

if not exist "%UNITY_EXE%" (
    echo Unity %UNITY_VERSION% was not found at:
    echo   %UNITY_EXE%
    echo Set the UNITY_EXE environment variable to your Unity.exe path and run again.
    exit /b 1
)

set "TARGET=%~1"
if "%TARGET%"=="" set "TARGET=all"

if not exist "%~dp0Builds" mkdir "%~dp0Builds"

if /i "%TARGET%"=="windows" (
    call :run BuildWindows || exit /b 1
) else if /i "%TARGET%"=="apk" (
    call :run BuildAndroidApk || exit /b 1
) else if /i "%TARGET%"=="aab" (
    call :run BuildAndroidBundle || exit /b 1
) else if /i "%TARGET%"=="all" (
    call :run BuildWindows || exit /b 1
    call :run BuildAndroidApk || exit /b 1
) else (
    echo Usage: build.bat [windows ^| apk ^| aab ^| all]
    exit /b 1
)

echo.
echo Done. Output is in "%~dp0Builds"
exit /b 0

:run
echo.
echo === %1 ===
"%UNITY_EXE%" -batchmode -quit -projectPath "%~dp0." -executeMethod BuildTools.%1 -logFile "%~dp0Builds\%1.log"
if errorlevel 1 (
    echo %1 failed. Close the Unity editor if it has this project open, then check:
    echo   %~dp0Builds\%1.log
    exit /b 1
)
exit /b 0
