# Builds dist\BCNWallet.exe with the C# compiler that ships with Windows (.NET Framework 4.8).
# Nothing to install: run from PowerShell in this folder:  .\build.ps1
#   -Release   also makes release\BCNWallet-<ver>-win-portable.zip and release\BCNWallet-<ver>-setup.exe
#              (both include walletd.exe + bytecoind.exe from .\node, fetched by get-node.ps1 if missing)
param([switch]$Release)
$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$fw = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$csc = Join-Path $fw 'csc.exe'
if (-not (Test-Path $csc)) { throw ".NET Framework 4.x compiler not found: $csc" }

$refs = @('System.dll', 'System.Core.dll', 'System.Xaml.dll', 'System.Xml.dll', 'WPF\WindowsBase.dll', 'WPF\PresentationCore.dll',
          'WPF\PresentationFramework.dll', 'System.Web.Extensions.dll', 'System.Management.dll', 'System.IO.Compression.dll', 'System.IO.Compression.FileSystem.dll',
          'System.Windows.Forms.dll', 'System.Drawing.dll') | ForEach-Object { '/r:' + (Join-Path $fw $_) }

$obj = Join-Path $root 'obj'
$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force $obj, $dist, (Join-Path $root 'assets') | Out-Null

# 1. icon from the vector logo
$ico = Join-Path $root 'assets\app.ico'
& $csc /nologo /target:exe /platform:anycpu /out:"$obj\MakeIcon.exe" $refs (Join-Path $root 'tools\MakeIcon.cs')
if ($LASTEXITCODE -ne 0) { throw 'MakeIcon build failed' }
& "$obj\MakeIcon.exe" $ico (Join-Path $root 'assets\icon-256.png')
if ($LASTEXITCODE -ne 0) { throw 'icon render failed' }

# 2. the application
$src = Get-ChildItem (Join-Path $root 'src') -Filter *.cs | ForEach-Object { $_.FullName }
$langs = Get-ChildItem (Join-Path $root 'src\lang') -Filter *.txt | ForEach-Object { "/resource:$($_.FullName),BcnWallet.lang.$($_.Name)" }
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /codepage:65001 `
    /out:"$dist\BCNWallet.exe" /win32icon:"$ico" /win32manifest:"$root\src\app.manifest" `
    /resource:"$root\src\Styles.xaml,BcnWallet.Styles.xaml" $langs $refs $src
if ($LASTEXITCODE -ne 0) { throw 'BCNWallet build failed' }

# 3. side files; node binaries if they were fetched
foreach ($f in @('get-node.ps1', 'LICENSE', 'COPYING') + (Get-ChildItem $root -Filter 'README*.md' | ForEach-Object Name)) { Copy-Item (Join-Path $root $f) $dist -Force }
$node = Join-Path $root 'node'
if (Test-Path (Join-Path $node 'walletd.exe')) {
    New-Item -ItemType Directory -Force (Join-Path $dist 'node') | Out-Null
    Copy-Item (Join-Path $node '*') (Join-Path $dist 'node') -Force
}
Write-Host "OK: $dist\BCNWallet.exe"

if (-not $Release) { return }

# 4. release: portable zip + installer, both with the daemons
if (-not (Test-Path (Join-Path $node 'walletd.exe'))) { & (Join-Path $root 'get-node.ps1') }
$version = (Get-Item "$dist\BCNWallet.exe").VersionInfo.ProductVersion
$stage = Join-Path $obj 'release\BCNWallet'
if (Test-Path $stage) { Remove-Item -Recurse -Force $stage }
New-Item -ItemType Directory -Force $stage, "$stage\node" | Out-Null
foreach ($f in @('BCNWallet.exe', 'get-node.ps1', 'LICENSE', 'COPYING') + (Get-ChildItem $dist -Filter 'README*.md' | ForEach-Object Name)) { Copy-Item (Join-Path $dist $f) $stage }
foreach ($f in 'walletd.exe', 'bytecoind.exe', 'README.txt') { Copy-Item (Join-Path $node $f) "$stage\node" }

$rel = Join-Path $root 'release'
New-Item -ItemType Directory -Force $rel | Out-Null

# 4a. installer payload: the program without data\ (installed copies keep data in %APPDATA%\BCN Wallet)
$payload = Join-Path $obj 'payload.zip'
if (Test-Path $payload) { Remove-Item $payload }
Compress-Archive -Path "$stage\*" -DestinationPath $payload
& $csc /nologo /target:winexe /platform:anycpu /optimize+ /utf8output /codepage:65001 `
    /out:"$rel\BCNWallet-$version-setup.exe" /win32icon:"$ico" /win32manifest:"$root\setup\setup.manifest" `
    /resource:"$payload,payload.zip" $refs (Join-Path $root 'setup\Setup.cs')
if ($LASTEXITCODE -ne 0) { throw 'Setup build failed' }

# 4b. portable: the same files plus an empty data\ folder (its presence switches on portable mode)
New-Item -ItemType Directory -Force "$stage\data" | Out-Null
Set-Content "$stage\data\README.txt" -Encoding UTF8 -Value @(
    'Portable mode: while this folder exists, BCN Wallet keeps settings, wallet files, the wallet cache and (in full-node mode) the blockchain here.',
    'Delete it to use %APPDATA%\BCN Wallet instead. Back up the *.wallet files and your 24 words!',
    'Портативний режим: поки ця папка існує, BCN Wallet зберігає тут налаштування, файли гаманців, кеш і (у режимі повного вузла) блокчейн.',
    'Видаліть її, щоб використовувати %APPDATA%\BCN Wallet. Зробіть резервну копію файлів *.wallet і запишіть 24 слова!')
$zip = Join-Path $rel "BCNWallet-$version-win-portable.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path $stage -DestinationPath $zip

$sums = foreach ($f in @($zip, "$rel\BCNWallet-$version-setup.exe")) {
    "$((Get-FileHash $f -Algorithm SHA256).Hash.ToLowerInvariant())  $(Split-Path $f -Leaf)"
}
Set-Content (Join-Path $rel 'SHA256SUMS') -Encoding Ascii -Value $sums
$sums | ForEach-Object { Write-Host $_ }
Write-Host "OK: $rel"
