# Windows integration checks. No prerequisite installer is downloaded or run.
[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$SetupPath)
$ErrorActionPreference = 'Stop'
$SetupPath = (Resolve-Path $SetupPath).Path
$testRoot = Join-Path $PWD ('artifacts/setup-check-' + [Guid]::NewGuid().ToString('N'))
$app = Join-Path $testRoot 'Editor with spaces'
$originalLocal = $env:LOCALAPPDATA
$originalPython = $env:FM26_PYTHON
try {
    New-Item $testRoot -ItemType Directory -Force | Out-Null
    # No consent => no Python/.NET download. Silent installs are also opt-in.
    $process = Start-Process $SetupPath -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS= /DIR=`"$app`" /LOG=`"$testRoot/install.log`"" -Wait -PassThru
    if ($process.ExitCode -ne 0) { throw "Setup failed: $($process.ExitCode)" }
    foreach ($file in @('FM26SimpleEditor.exe', 'en/FM26SimpleEditor.resources.dll', 'editor.py', 'requirements.txt', 'FM26SimpleEditor-source.zip', 'installer/Install-Prerequisites.ps1', 'unins000.exe')) {
        if (-not (Test-Path (Join-Path $app $file))) { throw "Missing installed file: $file" }
    }
    if (Test-Path (Join-Path $originalLocal 'FM26SimpleEditor/installer-prerequisites.log')) { throw 'Unselected tasks unexpectedly ran.' }
    # Exercise real interpreter discovery, dedicated venv, pip and final import checks.
    # Redirect all editor state away from the runner's real profile.
    $env:FM26_PYTHON = (Get-Command python.exe).Source
    $env:LOCALAPPDATA = Join-Path $testRoot 'Local profile'
    $helper = Join-Path $app 'installer/Install-Prerequisites.ps1'
    foreach ($attempt in 1..2) {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -File $helper -AppDirectory $app -ConfigurePython
        if ($LASTEXITCODE -ne 0) { throw "Python preparation failed on attempt $attempt" }
    }
    $environments = @(Get-ChildItem (Join-Path $env:LOCALAPPDATA 'FM26SimpleEditor/runtimes') -Directory -Filter 'python-*')
    if ($environments.Count -ne 1) { throw 'Expected one reusable dedicated environment.' }
    $exe = Join-Path $environments[0].FullName 'Scripts/python.exe'
    $probe = & $exe -I -X utf8 (Join-Path $app 'runtime_probe.py') (Join-Path $app 'requirements.txt') | ConvertFrom-Json
    if (-not $probe.ready) { throw 'Installed environment is not ready.' }
    $env:LOCALAPPDATA = $originalLocal
    $config = Join-Path $app 'FM26SimpleEditor.config.json'
    '{"language":"en"}' | Set-Content $config
    $process = Start-Process $SetupPath -ArgumentList "/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /TASKS= /DIR=`"$app`"" -Wait -PassThru
    if ($process.ExitCode -ne 0 -or (Get-Content $config -Raw).Trim() -ne '{"language":"en"}') { throw 'Upgrade did not preserve local settings.' }
    $process = Start-Process (Join-Path $app 'unins000.exe') -ArgumentList '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART' -Wait -PassThru
    if ($process.ExitCode -ne 0 -or (Test-Path (Join-Path $app 'FM26SimpleEditor.exe'))) { throw 'Uninstall failed.' }
    if (-not (Test-Path $config)) { throw 'Uninstall removed user-created settings.' }
    Write-Host 'Setup, opt-in behavior, dedicated Python environment, repeat setup, upgrade and uninstall passed.'
} finally {
    $env:LOCALAPPDATA = $originalLocal
    $env:FM26_PYTHON = $originalPython
    Remove-Item $testRoot -Recurse -Force -ErrorAction SilentlyContinue
}
