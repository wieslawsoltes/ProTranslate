param(
    [string]$Configuration = "Release",
    [string]$ResultsDir = "artifacts/test-results/leak",
    [int]$HeartbeatSeconds = 60,
    [switch]$SkipMaui,
    [switch]$SkipWindowsAdapters
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$resultsDirFull = Join-Path $repoRoot $ResultsDir
New-Item -ItemType Directory -Path $resultsDirFull -Force | Out-Null

$dotnet = (Get-Command dotnet).Source
if (-not $dotnet) {
    throw "dotnet not found in PATH."
}

function Invoke-DotNet {
    param(
        [Parameter(Mandatory = $true)]
        [string[]]$Arguments,
        [hashtable]$Environment = @{}
    )

    Write-Host "dotnet $($Arguments -join ' ')"
    $startInfo = [System.Diagnostics.ProcessStartInfo]::new($dotnet)
    foreach ($argument in $Arguments) {
        $startInfo.ArgumentList.Add($argument)
    }

    $startInfo.UseShellExecute = $false
    foreach ($key in $Environment.Keys) {
        $startInfo.Environment[$key] = [string]$Environment[$key]
    }

    $process = [System.Diagnostics.Process]::Start($startInfo)
    if ($null -eq $process) {
        throw "Failed to start dotnet."
    }

    $started = Get-Date
    if ($HeartbeatSeconds -gt 0) {
        while (-not $process.WaitForExit($HeartbeatSeconds * 1000)) {
            $elapsed = (Get-Date) - $started
            Write-Host ("Leak test command still running... elapsed {0:hh\:mm\:ss}" -f $elapsed)
        }
    } else {
        $process.WaitForExit()
    }

    $process.Refresh()
    if ($process.ExitCode -ne 0) {
        throw "dotnet exited with code $($process.ExitCode)."
    }
}

function Invoke-LeakTestProject {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,
        [Parameter(Mandatory = $true)]
        [string]$LogName,
        [hashtable]$Environment = @{}
    )

    $projectFull = Join-Path $repoRoot $Project
    if (-not (Test-Path $projectFull)) {
        throw "Test project not found at $projectFull"
    }

    Invoke-DotNet @(
        "test",
        $projectFull,
        "-c",
        $Configuration,
        "--logger",
        "console;verbosity=detailed",
        "--logger",
        "trx;LogFileName=$LogName.trx",
        "--results-directory",
        $resultsDirFull,
        "--filter",
        "FullyQualifiedName~Leak"
    ) $Environment
}

Write-Host "dotnet: $dotnet"
Write-Host "Repository: $repoRoot"
Write-Host "Results dir: $resultsDirFull"

Invoke-LeakTestProject "tests/ProTranslate.Tests/ProTranslate.Tests.csproj" "protranslate-core-leak-tests"
Invoke-LeakTestProject "tests/ProTranslate.Avalonia.Tests/ProTranslate.Avalonia.Tests.csproj" "protranslate-avalonia-leak-tests"
Invoke-LeakTestProject "tests/ProTranslate.Uno.Tests/ProTranslate.Uno.Tests.csproj" "protranslate-uno-leak-tests"

if ($IsMacOS -and -not $SkipMaui) {
    Invoke-DotNet @(
        "build",
        (Join-Path $repoRoot "src/ProTranslate.Maui/ProTranslate.Maui.csproj"),
        "-c",
        $Configuration
    )
    Invoke-LeakTestProject `
        "tests/ProTranslate.Maui.Tests/ProTranslate.Maui.Tests.csproj" `
        "protranslate-maui-leak-tests" `
        @{ "PROTRANSLATE_REQUIRE_MAUI_LEAKS" = "1" }
} else {
    Write-Host "Skipping MAUI leak tests on this OS."
}

if ($IsWindows -and -not $SkipWindowsAdapters) {
    Invoke-LeakTestProject "tests/ProTranslate.Wpf.Tests/ProTranslate.Wpf.Tests.csproj" "protranslate-wpf-leak-tests"
    Invoke-DotNet @(
        "build",
        (Join-Path $repoRoot "src/ProTranslate.WinUI/ProTranslate.WinUI.csproj"),
        "-c",
        $Configuration,
        "-p:CopyLocalLockFileAssemblies=true"
    )
    Invoke-LeakTestProject `
        "tests/ProTranslate.WinUI.Tests/ProTranslate.WinUI.Tests.csproj" `
        "protranslate-winui-leak-tests" `
        @{ "PROTRANSLATE_REQUIRE_WINUI_LEAKS" = "1" }
} else {
    Write-Host "Skipping Windows-only WPF/WinUI leak tests on this OS."
}
