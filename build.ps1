$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"
$root = $PSScriptRoot
$buildDir = Join-Path $root ".build"
$dist = Join-Path $root "dist"
New-Item -ItemType Directory -Force -Path $buildDir, $dist | Out-Null

$nsisZip = Join-Path $buildDir "nsis-3.13.zip"
$nsisUrl = "https://downloads.sourceforge.net/project/nsis/NSIS%203/3.13/nsis-3.13.zip"
$nsisSha256 = "BA63DFFC4410EE89193E1CB5A41989991BD77C61068DA17E3156D136B7B0B3D8"
$nsisRoot = Join-Path $buildDir "nsis-3.13"
$makensis = Join-Path $nsisRoot "makensis.exe"
if (-not (Test-Path -LiteralPath $nsisZip)) {
    Invoke-WebRequest -UseBasicParsing -Uri $nsisUrl -OutFile $nsisZip
}
if ((Get-FileHash -LiteralPath $nsisZip -Algorithm SHA256).Hash -ne $nsisSha256) {
    throw "NSIS archive checksum mismatch."
}
if (-not (Test-Path -LiteralPath $makensis)) {
    Expand-Archive -LiteralPath $nsisZip -DestinationPath $buildDir -Force
}

$tesseractInstaller = Join-Path $buildDir "TesseractSetup.exe"
$tesseractUrl = "https://github.com/tesseract-ocr/tesseract/releases/download/5.5.3/tesseract-ocr-w64-setup-5.5.3.20260724.exe"
$tesseractSha256 = "BEE9E3434BD94FD65387D9BE28CD467A41F61B1275383B55B0F59A1331270AE4"
if (-not (Test-Path -LiteralPath $tesseractInstaller)) {
    Invoke-WebRequest -UseBasicParsing -Uri $tesseractUrl -OutFile $tesseractInstaller
}
if ((Get-FileHash -LiteralPath $tesseractInstaller -Algorithm SHA256).Hash -ne $tesseractSha256) {
    throw "Tesseract installer checksum mismatch."
}

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$appExe = Join-Path $buildDir "AzuraGameTranslate.exe"
if (-not (Test-Path -LiteralPath $csc)) {
    throw ".NET Framework C# compiler was not found. Install the .NET Framework 4.x developer tools."
}
$references = @(
    "/r:System.dll",
    "/r:System.Core.dll",
    "/r:System.Drawing.dll",
    "/r:System.Net.Http.dll",
    "/r:Microsoft.CSharp.dll",
    "/r:System.Web.Extensions.dll",
    "/r:System.Windows.Forms.dll"
)
$compilerArgs = @("/nologo", "/target:winexe", "/platform:x64", "/optimize+", "/out:$appExe") + $references + (Join-Path $root "AzuraGameTranslate.cs")
& $csc @compilerArgs
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $appExe)) {
    throw "Application compilation failed."
}

Push-Location $root
try {
    & $makensis /V3 "installer.nsi"
    if ($LASTEXITCODE -ne 0) { throw "Installer compilation failed." }
} finally {
    Pop-Location
}

$setup = Join-Path $dist "AzuraGameTranslate-Setup-v1.0.0.exe"
$sourceZip = Join-Path $dist "AzuraGameTranslate-Source-v1.0.0.zip"
$sourceFiles = @(
    "AzuraGameTranslate.cs",
    "installer.nsi",
    "build.ps1",
    "README.md",
    "LICENSE",
    "THIRD-PARTY-NOTICES.md",
    "Tesseract-Apache-2.0.txt"
) | ForEach-Object { Join-Path $root $_ }
Compress-Archive -LiteralPath $sourceFiles -DestinationPath $sourceZip -Force
Get-Item $setup, $sourceZip | Select-Object FullName, Length
Get-FileHash -LiteralPath $setup -Algorithm SHA256 | Format-List