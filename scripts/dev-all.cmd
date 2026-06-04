@echo off
setlocal enabledelayedexpansion

echo ============================================
echo  ModelForge — 全栈开发环境启动
echo ============================================
echo.

pushd "%~dp0.."

set START_DIR=%CD%
set TIMEOUT_BACKEND=12
set TIMEOUT_SIDECAR=5

:: ── 1. 启动 Backend (:5095) ──
echo [1/3] 启动 Backend: http://localhost:5095
start "ModelForge-Backend" cmd /c "dotnet run --project src\backend\ModelForge.Backend\ModelForge.Backend.csproj & pause"
echo   等待 Backend 就绪...
timeout /t 3 /nobreak >nul

:: ── 2. 启动 Sidecar (:5200) ──
echo [2/3] 启动 Sidecar: http://localhost:5200
start "ModelForge-Sidecar" cmd /c "dotnet run --project src\sidecar\ModelForge.Sidecar\ModelForge.Sidecar.csproj & pause"
echo   等待 Sidecar 就绪...
timeout /t 2 /nobreak >nul

:: ── 3. 启动 Web Add-in (:5173) ──
echo [3/3] 启动 Web Add-in: http://127.0.0.1:5173
cd /d "%START_DIR%\src\web"
start "ModelForge-Web" cmd /c "npm run dev & pause"
cd /d "%START_DIR%"

echo.
echo ============================================
echo  所有服务已启动：
echo    Backend  : http://localhost:5095
echo    Sidecar  : http://localhost:5200
echo    Web      : http://127.0.0.1:5173
echo ============================================
echo.
echo 按任意键停止所有服务...
pause >nul

echo 正在停止服务...
taskkill /FI "WINDOWTITLE eq ModelForge-*" /T /F >nul 2>&1
echo 已停止。

popd
endlocal
exit /b 0
