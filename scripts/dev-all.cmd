@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  ModelForge dev stack
echo ============================================
echo.

pushd "%~dp0.."
set "START_DIR=%CD%"

echo [preflight] Checking Office runtime
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0check-office-runtime.ps1" -SkipSidecar
if errorlevel 1 (
    echo.
    echo Office runtime preflight failed. Close WPS/Kingsoft Office and rerun this script.
    popd
    endlocal
    exit /b 1
)
echo.

echo [1/3] Starting Backend: http://localhost:5095
start "ModelForge-Backend" cmd /k "cd /d "%START_DIR%" && dotnet run --project src\backend\ModelForge.Backend\ModelForge.Backend.csproj --launch-profile ModelForge.Backend"
timeout /t 3 /nobreak >nul

echo [2/3] Starting Sidecar: http://localhost:5200
start "ModelForge-Sidecar" cmd /k "cd /d "%START_DIR%" && dotnet run --project src\sidecar\ModelForge.Sidecar\ModelForge.Sidecar.csproj"
timeout /t 2 /nobreak >nul

echo [3/3] Starting Web Add-in: http://localhost:5173
start "ModelForge-Web" cmd /k "cd /d "%START_DIR%\src\web" && npm run dev"

echo.
echo ============================================
echo  Services are starting in separate windows.
echo    Backend : http://localhost:5095
echo    Sidecar : http://localhost:5200
echo    Web     : http://localhost:5173
echo ============================================
echo.
echo Keep those windows open while testing Office Ribbon.
echo Close the three ModelForge-* windows when finished.

popd
endlocal
