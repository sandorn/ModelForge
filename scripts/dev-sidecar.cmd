@echo off
setlocal

pushd "%~dp0.."
echo [ModelForge] Starting Sidecar: http://localhost:5200
dotnet run --project src\sidecar\ModelForge.Sidecar\ModelForge.Sidecar.csproj
set EXIT_CODE=%ERRORLEVEL%
popd
exit /b %EXIT_CODE%
