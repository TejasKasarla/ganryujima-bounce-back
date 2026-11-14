[CmdletBinding()]
param(
    [string]$OfflinePackage,
    [string]$OfflineModel
)

$ErrorActionPreference = 'Stop'
$version = '0.10.21'
$faceDirectory = Split-Path $PSScriptRoot -Parent
$vendor = Join-Path $faceDirectory 'Web\vendor'
$models = Join-Path $faceDirectory 'Models'
$temporary = Join-Path ([System.IO.Path]::GetTempPath()) ('ganryujima-face-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $vendor, $models, $temporary | Out-Null

try {
    $archive = Join-Path $temporary 'tasks-vision.tgz'
    if ($OfflinePackage) {
        Copy-Item -LiteralPath $OfflinePackage -Destination $archive
    } else {
        Invoke-WebRequest -Uri "https://registry.npmjs.org/@mediapipe/tasks-vision/-/tasks-vision-$version.tgz" -OutFile $archive
    }
    & tar -xf $archive -C $temporary
    if ($LASTEXITCODE -ne 0) { throw 'Could not extract the MediaPipe npm package.' }
    $package = Join-Path $temporary 'package'
    $metadata = Get-Content -LiteralPath (Join-Path $package 'package.json') -Raw | ConvertFrom-Json
    if ($metadata.version -ne $version) { throw "Expected tasks-vision $version, got $($metadata.version)." }
    Copy-Item -LiteralPath (Join-Path $package 'vision_bundle.mjs') -Destination $vendor -Force
    Copy-Item -LiteralPath (Join-Path $package 'wasm') -Destination $vendor -Recurse -Force
    Copy-Item -LiteralPath (Join-Path $package 'package.json') -Destination $vendor -Force
    Get-ChildItem -LiteralPath $package -Filter '*LICENSE*' | Copy-Item -Destination $vendor -Force

    $model = Join-Path $models 'face_landmarker.task'
    if ($OfflineModel) {
        Copy-Item -LiteralPath $OfflineModel -Destination $model -Force
    } elseif (!(Test-Path -LiteralPath $model)) {
        Invoke-WebRequest -Uri 'https://storage.googleapis.com/mediapipe-models/face_landmarker/face_landmarker/float16/1/face_landmarker.task' -OutFile $model
    }
    if ((Get-Item -LiteralPath $model).Length -lt 1000000) { throw 'The downloaded model is incomplete.' }
    $modelHash = Get-FileHash -LiteralPath $model -Algorithm SHA256
    if ($modelHash.Hash -ne '64184E229B263107BC2B804C6625DB1341FF2BB731874B0BCC2FE6544E0BC9FF') { throw 'The model does not match the pinned official float16/1 face landmarker.' }
    Write-Output "Ready: MediaPipe $version and local face model. No runtime network downloads are used."
    $modelHash | Select-Object Algorithm, Hash
} finally {
    Remove-Item -LiteralPath $temporary -Recurse -Force
}