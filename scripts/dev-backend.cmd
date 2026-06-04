@echo off
setlocal

pushd "%~dp0.."
echo [ModelForge] Starting backend bridge: http://localhost:5095
dotnet run --project src\backend\ModelForge.Backend\ModelForge.Backend.csproj --launch-profile ModelForge.Backend
set EXIT_CODE=%ERRORLEVEL%
popd
exit /b %EXIT_CODE%