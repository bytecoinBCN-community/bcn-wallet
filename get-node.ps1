# Downloads walletd.exe and bytecoind.exe 3.5.1 (official bcndev release, Windows x64) into .\node
# The archive is installed only if its SHA-256 matches the value pinned here (GitHub publishes no checksum for it;
# the value was taken from the release asset on 2026-11-15, size 25 601 161 bytes).
# BCN Wallet does the same by itself on first start, so this script is only needed for builds from source.
#
#   powershell -ExecutionPolicy Bypass -File .\get-node.ps1
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

$url = 'https://github.com/bcndev/bytecoin/releases/download/v3.5.1/bytecoin-daemons-3.5.1-win64.zip'
$sha = 'daa6a8a3b5c3aec7abc05a1f001abe1c0afd2d540ae7cb8fe9dc6dfdb72aec96'
$dest = Join-Path $PSScriptRoot 'node'
$tmp = Join-Path $env:TEMP ('bcn-wallet-node-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $dest, $tmp | Out-Null
try {
    $zip = Join-Path $tmp 'daemons.zip'
    Write-Host "downloading $url"
    Invoke-WebRequest -Uri $url -OutFile $zip -Headers @{ 'User-Agent' = 'bcn-wallet-setup' } -UseBasicParsing
    $actual = (Get-FileHash $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actual -ne $sha) { throw "SHA-256 mismatch: expected $sha, got $actual" }
    Write-Host "SHA-256 OK  $actual"
    $x = Join-Path $tmp 'x'
    Expand-Archive -Path $zip -DestinationPath $x -Force
    foreach ($n in 'walletd.exe', 'bytecoind.exe') {
        $f = Get-ChildItem $x -Recurse -File -Filter $n | Select-Object -First 1
        if (-not $f) { throw "$n not found in the archive" }
        Copy-Item $f.FullName (Join-Path $dest $n) -Force
    }
    Set-Content (Join-Path $dest 'README.txt') -Encoding UTF8 -Value @(
        'walletd.exe and bytecoind.exe 3.5.1 from the official Bytecoin release (bcndev), unchanged.',
        'Source and licence (LGPL-3.0): https://github.com/bcndev/bytecoin/releases/tag/v3.5.1',
        "Archive SHA-256: $sha")
    Write-Host "Done: $dest"
}
finally {
    Remove-Item -Recurse -Force $tmp -ErrorAction SilentlyContinue
}
