@echo off
setlocal

pushd "%~dp0.."
echo [ModelForge] Building backend project...
dotnet build src\backend\ModelForge.Backend\ModelForge.Backend.csproj
if errorlevel 1 (
  popd
  exit /b 1
)

echo [ModelForge] Running backend smoke tests...
dotnet run --project tests\backend\ModelForge.Backend.SmokeTests\ModelForge.Backend.SmokeTests.csproj
set EXIT_CODE=%ERRORLEVEL%
popd
exit /b %EXIT_CODE%