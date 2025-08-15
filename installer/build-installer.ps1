param(
  [string]$Version = "1.0.0"
)

$ErrorActionPreference = "Stop"

# 1) Publish self-contained
dotnet publish ../Nuform.App/Nuform.App.csproj \
  -c Release \
  /p:PublishProfile=FolderSelfContained

# 2) Check Inno Setup Compiler (ISCC.exe) in PATH
$iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue)
if (-not $iscc) {
  Write-Error "Inno Setup Compiler (ISCC.exe) not found in PATH. Install Inno Setup and add ISCC to PATH."
}

# 3) Copy config template next to .iss for Files section
Copy-Item -Force ./config.template.json ./config.json

# 4) Bump version in .iss
(Get-Content ./nuform_estimator.iss) \
  -replace 'AppVersion ".*"', 'AppVersion "' + $Version + '"' \
  | Set-Content ./nuform_estimator.iss

# 5) Compile installer
& iscc.exe .\nuform_estimator.iss

Write-Host "Done. See output EXE in installer\ folder."
