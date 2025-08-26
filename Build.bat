@echo off
setlocal
pushd %~dp0

REM Build and publish the WPF app (Release, self-contained=false)
echo Building Nuform.App...
dotnet publish Nuform.App\Nuform.App.csproj -c Release -o publish

if %errorlevel% neq 0 (
  echo Build failed. Ensure .NET 8 SDK is installed.
  pause
  exit /b 1
)

echo Done. Find the app in: %cd%\publish
pause
