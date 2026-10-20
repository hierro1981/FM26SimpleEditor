# Run by Setup only after the user selects the corresponding optional task.
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$AppDirectory,
    [switch]$ConfigurePython,
    [switch]$InstallDesktopRuntime
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12

function Invoke-Checked([string]$Executable, [string[]]$Arguments) {
    & $Executable @Arguments | Out-Host
    if ($LASTEXITCODE -ne 0) { throw "$Executable failed (exit $LASTEXITCODE)." }
}

function Get-PythonInfo([string]$Executable) {
    if (-not $Executable -or -not (Test-Path -LiteralPath $Executable -PathType Leaf)) { return $null }
    try {
        $json = & $Executable -I -X utf8 (Join-Path $AppDirectory 'runtime_probe.py') (Join-Path $AppDirectory 'requirements.txt') 2>$null
        if ($LASTEXITCODE -ne 0) { return $null }
        $info = $json | ConvertFrom-Json
        if ($info.PSObject.Properties['supported'] -and $info.supported -and -not $info.PSObject.Properties['error']) { return $info }
    } catch { Write-Host "Skipping unusable Python: $Executable" }
    return $null
}

function Find-Python {
    $paths = [Collections.Generic.List[string]]::new()
    if ($env:FM26_PYTHON) { $paths.Add($env:FM26_PYTHON.Trim('"')) }
    $root = Join-Path $env:LOCALAPPDATA 'FM26SimpleEditor/runtimes'
    if (Test-Path $root) {
        Get-ChildItem $root -Directory -Filter 'python-*' | Sort-Object Name -Descending | ForEach-Object {
            $paths.Add((Join-Path $_.FullName 'Scripts/python.exe'))
        }
    }
    foreach ($hive in @([Microsoft.Win32.RegistryHive]::CurrentUser, [Microsoft.Win32.RegistryHive]::LocalMachine)) {
        foreach ($view in @([Microsoft.Win32.RegistryView]::Registry64, [Microsoft.Win32.RegistryView]::Registry32)) {
            $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey($hive, $view)
            try {
                $versions = $base.OpenSubKey('Software\Python\PythonCore')
                if ($null -eq $versions) { continue }
                try {
                    foreach ($version in ($versions.GetSubKeyNames() | Sort-Object -Descending)) {
                        $key = $versions.OpenSubKey("$version\InstallPath")
                        if ($null -eq $key) { continue }
                        try {
                            $exe = $key.GetValue('ExecutablePath')
                            if ($exe) { $paths.Add([string]$exe) }
                            elseif ($key.GetValue('')) { $paths.Add((Join-Path $key.GetValue('') 'python.exe')) }
                        } finally { $key.Dispose() }
                    }
                } finally { $versions.Dispose() }
            } finally { $base.Dispose() }
        }
    }
    $localPython = Join-Path $env:LOCALAPPDATA 'Programs/Python'
    if (Test-Path $localPython) {
        Get-ChildItem $localPython -Directory -Filter 'Python*' | Sort-Object Name -Descending | ForEach-Object {
            $paths.Add((Join-Path $_.FullName 'python.exe'))
        }
    }
    foreach ($name in @('python.exe', 'python3.exe')) {
        Get-Command $name -All -ErrorAction SilentlyContinue | Where-Object { $_.Source -notlike '*\WindowsApps\*' } | ForEach-Object { $paths.Add($_.Source) }
    }
    if (Get-Command py.exe -ErrorAction SilentlyContinue) {
        & py.exe --list-paths 2>$null | ForEach-Object {
            if ($_ -match '(?<path>[A-Za-z]:\\.*python(?:\d+(?:\.\d+)?)?\.exe)\s*$') { $paths.Add($Matches.path.Trim()) }
        }
    }
    foreach ($path in ($paths | Select-Object -Unique)) {
        $info = Get-PythonInfo $path
        if ($null -ne $info) { return $info }
    }
    return $null
}

function Save-VerifiedInstaller([string]$Url, [string]$Destination, [string]$Publisher, [string]$Sha512 = '') {
    if (-not $Url.StartsWith('https://', [StringComparison]::OrdinalIgnoreCase)) { throw 'HTTPS download required.' }
    Invoke-WebRequest -UseBasicParsing -Uri $Url -OutFile $Destination -TimeoutSec 180
    if ($Sha512 -and (Get-FileHash -LiteralPath $Destination -Algorithm SHA512).Hash -ne $Sha512) {
        throw 'Installer checksum mismatch.'
    }
    $signature = Get-AuthenticodeSignature -LiteralPath $Destination
    if ($signature.Status -ne 'Valid' -or $signature.SignerCertificate.Subject -notmatch $Publisher) {
        throw 'Installer publisher/signature verification failed.'
    }
}

function Start-Installer([string]$Path, [string]$Arguments) {
    $process = Start-Process -FilePath $Path -ArgumentList $Arguments -Wait -PassThru
    if ($process.ExitCode -eq 3010) { Write-Host 'A Windows restart may be required.' }
    elseif ($process.ExitCode -ne 0) { throw "Prerequisite installer failed (exit $($process.ExitCode))." }
}

function Test-DesktopRuntime {
    $base = [Microsoft.Win32.RegistryKey]::OpenBaseKey([Microsoft.Win32.RegistryHive]::LocalMachine, [Microsoft.Win32.RegistryView]::Registry64)
    try {
        $key = $base.OpenSubKey('SOFTWARE\dotnet\Setup\InstalledVersions\x64\sharedfx\Microsoft.WindowsDesktop.App')
        if ($null -eq $key) { return $false }
        try { return @($key.GetValueNames() | Where-Object { $_ -match '^8\.0\.' }).Count -gt 0 }
        finally { $key.Dispose() }
    } finally { $base.Dispose() }
}

function Install-Prerequisites {
    $work = Join-Path ([IO.Path]::GetTempPath()) ('FM26-setup-' + [Guid]::NewGuid().ToString('N'))
    New-Item $work -ItemType Directory | Out-Null
    try {
        if ($InstallDesktopRuntime -and -not (Test-DesktopRuntime)) {
            Write-Host 'Downloading .NET 8 Desktop Runtime x64 from Microsoft...'
            $metadata = Invoke-RestMethod -Uri 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json' -TimeoutSec 60
            $latest = $metadata.'latest-release'
            $release = @($metadata.releases | Where-Object { $_.'release-version' -eq $latest })[0]
            $file = @($release.windowsdesktop.files | Where-Object { $_.rid -eq 'win-x64' -and $_.url -like '*.exe' })[0]
            $uri = [Uri]$file.url
            if ($uri.Host -notin @('builds.dotnet.microsoft.com', 'download.visualstudio.microsoft.com')) { throw 'Unexpected .NET download host.' }
            $installer = Join-Path $work 'windowsdesktop-runtime.exe'
            Save-VerifiedInstaller $file.url $installer 'O=Microsoft Corporation(?:,|$)' $file.hash
            Start-Installer $installer '/install /passive /norestart'
            if (-not (Test-DesktopRuntime)) { throw '.NET Desktop Runtime is still unavailable; restart Windows if requested.' }
        }
        if ($ConfigurePython) {
            $python = Find-Python
            if ($null -eq $python) {
                Write-Host 'Downloading Python 3.13.16 x64 from python.org...'
                $installer = Join-Path $work 'python-3.13.16-amd64.exe'
                Save-VerifiedInstaller 'https://www.python.org/ftp/python/3.13.16/python-3.13.16-amd64.exe' $installer 'O=Python Software Foundation(?:,|$)'
                Start-Installer $installer '/passive InstallAllUsers=0 Include_launcher=0 PrependPath=0 Include_test=0 Include_doc=0 Include_pip=1'
                $python = Find-Python
                if ($null -eq $python) { throw 'No compatible Python found after installation.' }
            }
            $version = [Version]$python.version
            $environment = Join-Path $env:LOCALAPPDATA "FM26SimpleEditor/runtimes/python-$($version.Major).$($version.Minor)"
            $exe = Join-Path $environment 'Scripts/python.exe'
            $root = Split-Path $environment -Parent
            New-Item $root -ItemType Directory -Force | Out-Null
            # Same lease and directory convention as PythonRuntimeSetup.cs.
            $lease = [IO.File]::Open((Join-Path $root 'setup.lock'), 'OpenOrCreate', 'ReadWrite', 'None')
            try {
                $ready = Get-PythonInfo $exe
                if ($null -eq $ready -or -not $ready.ready) {
                    $base = $python.baseExecutable
                    if (-not (Test-Path -LiteralPath $base)) { $base = $python.executable }
                    Invoke-Checked $base @('-I', '-X', 'utf8', '-m', 'venv', $environment)
                    Invoke-Checked $exe @('-I', '-X', 'utf8', '-m', 'ensurepip', '--upgrade')
                    Write-Host 'Installing editor dependencies in its dedicated environment...'
                    Invoke-Checked $exe @('-I', '-X', 'utf8', '-m', 'pip', '--isolated', 'install', '--index-url', 'https://pypi.org/simple', '--disable-pip-version-check', '--no-input', '--retries', '2', '--timeout', '30', '--requirement', (Join-Path $AppDirectory 'requirements.txt'))
                    $ready = Get-PythonInfo $exe
                    if ($null -eq $ready -or -not $ready.ready) { throw 'Python dependency verification failed.' }
                }
                Write-Host "Python ready: $exe"
            } finally { $lease.Dispose() }
        }
    } finally { Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue }
}

# Dot-source for dependency-free unit tests; running the script executes setup.
if ($MyInvocation.InvocationName -ne '.') {
    $logRoot = Join-Path $env:LOCALAPPDATA 'FM26SimpleEditor'
    New-Item $logRoot -ItemType Directory -Force | Out-Null
    $transcribing = $false
    try {
        Start-Transcript -Path (Join-Path $logRoot 'installer-prerequisites.log') -Force | Out-Null
        $transcribing = $true
        Install-Prerequisites
        exit 0
    } catch {
        Write-Host $_.Exception.Message
        exit 1
    } finally {
        if ($transcribing) { Stop-Transcript | Out-Null }
    }
}
