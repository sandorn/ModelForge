@echo off
setlocal

pushd "%~dp0..\src\web"
echo [ModelForge] Starting Web Add-in task pane: http://localhost:5173
if not exist node_modules (
  echo [ModelForge] node_modules not found. Please run npm install in src\web first.
  popd
  exit /b 1
)
npm run dev
set EXIT_CODE=%ERRORLEVEL%
popd
exit /b %EXIT_CODE%